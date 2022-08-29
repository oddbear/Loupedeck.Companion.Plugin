using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Loupedeck.CompanionPlugin.Extensions;
using Loupedeck.CompanionPlugin.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Websocket.Client;
using Websocket.Client.Models;

namespace Loupedeck.CompanionPlugin.Services
{
    public class CompanionClient : IDisposable
    {
        private readonly CompanionPlugin _plugin;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        internal event EventHandler<ResponseFillImage> FillImageResponse;
        
        private readonly WebsocketClient _client;

        public bool Connected => _client.IsRunning;

        private readonly List<object> _commandsOnReconnect = new List<object>();

        public CompanionClient(CompanionPlugin plugin)
        {
            _plugin = plugin;
            _client = CreateClient();
        }

        public void OnConnectCommand(object obj)
        {
            _commandsOnReconnect.Add(obj);
            if (Connected)
            {
                _client.SendObject(obj);
            }
        }

        public void SendCommand(string command, object obj)
        {
            _client.SendCommand(command, obj, _cancellationTokenSource.Token);
        }

        public void Start()
        {
            _ = _client.Start();
        }

        private WebsocketClient CreateClient()
        {
            var url = new Uri("ws://127.0.0.1:28492");
            var client = new WebsocketClient(url)
            {
                ErrorReconnectTimeout = TimeSpan.FromSeconds(5)
            };

            client.ReconnectionHappened.Subscribe(Connect);

            client.DisconnectionHappened.Subscribe(Disconnected, _cancellationTokenSource.Token);

            client.MessageReceived.Subscribe(
                onNext: Message,
                onError: Error,
                token: _cancellationTokenSource.Token);

            return client;
        }

        private void Connect(ReconnectionInfo reconnectionInfo)
        {
            switch (reconnectionInfo.Type)
            {
                case ReconnectionType.Initial:
                case ReconnectionType.Error:
                    _plugin.ConnectedStatus();

                    _client.SendCommand("version", new { version = 2 }, _cancellationTokenSource.Token);
                    _client.SendCommand("new_device", "2E1F407206FF4353B33D724CD1429550", _cancellationTokenSource.Token);

                    foreach (var command in _commandsOnReconnect)
                    {
                        _client.SendObject(command, _cancellationTokenSource.Token);
                    }
                    break;
            }
        }

        private void Message(ResponseMessage message)
        {
            try
            {
                if (message.Text is null)
                    return;

                if (message.MessageType == WebSocketMessageType.Text)
                    HandleJsonResponse(message.Text);
            }
            catch
            {
                //Ignore for now.
            }
        }
        
        private void Disconnected(DisconnectionInfo info)
        {
            if (info.Type == DisconnectionType.NoMessageReceived)
                return;

            _plugin.NotConnectedStatus();
        }

        private void Error(Exception exception)
        {
            _plugin.ErrorStatus(exception.Message);
        }
        
        private void HandleJsonResponse(string json)
        {
            var jObject = JsonConvert.DeserializeObject<JObject>(json);
            if (jObject is null)
                return;

            var response = jObject.Property("response");
            if (response != null)
            {
                var value = response.Value.Value<string>();
                var arguments = jObject.Property("arguments")?.Value;
                if (arguments is null)
                    return;

                switch (value)
                {
                    case "version":
                        var version = arguments.ToObject<ResponseVersion>();
                        break;
                    case "new_device":
                        var newDevice = arguments.ToObject<ResponseNewDevice>();
                        break;
                }
            }

            var command = jObject.Property("command");
            if (command != null)
            {
                var value = command.Value.Value<string>();
                var arguments = jObject.Property("arguments")?.Value;
                if (arguments is null)
                    return;

                switch (value)
                {
                    case "fillImage":
                        var fillImage = arguments.ToObject<ResponseFillImage>();
                        FillImageResponse?.Invoke(this, fillImage);
                        break;
                }
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _client?.Dispose();
        }
    }
}
