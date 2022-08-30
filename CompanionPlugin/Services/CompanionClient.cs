using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Loupedeck.CompanionPlugin.Extensions;
using Loupedeck.CompanionPlugin.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WatsonWebsocket;

namespace Loupedeck.CompanionPlugin.Services
{
    public class CompanionClient : IDisposable
    {
        private readonly CompanionPlugin _plugin;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        internal event EventHandler<ResponseFillImage> FillImageResponse;

        private Thread _thread;

        private WatsonWsClient _client;

        public bool Connected => _client?.Connected ?? false;

        private readonly List<object> _commandsOnReconnect = new List<object>();

        public CompanionClient(CompanionPlugin plugin)
        {
            _plugin = plugin;
            _thread = new Thread(Reconnect);
        }

        public void OnConnectCommand(object obj)
        {
            _commandsOnReconnect.Add(obj);
            if (Connected)
            {
                _client?.SendObject(obj);
            }
        }

        public void SendCommand(string command, object obj)
        {
            if (Connected)
            {
                _client?.SendCommand(command, obj, _cancellationTokenSource.Token);
            }
        }

        public void Start()
        {
            _thread.Start();
        }

        private WatsonWsClient CreateClient()
        {
            var uri = new Uri("ws://127.0.0.1:28492");
            var client = new WatsonWsClient(uri);

            client.ServerConnected += ClientOnOpen;
            client.MessageReceived += ClientOnMessage;
            client.ServerDisconnected += ClientOnClose;

            return client;
        }
        
        private void Reconnect()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    if (Connected)
                        continue;

                    _client?.Dispose();
                    _client = CreateClient();
                    _client.Start();

                    if (Connected)
                    {
                        _plugin.ConnectedStatus();
                    }
                    else
                    {
                        _plugin.NotConnectedStatus();
                    }
                }
                catch (Exception exception)
                {
                    if (exception.Message != "A series of reconnecting has failed.")
                    {
                        Trace.WriteLine($"{exception.GetType().Name}: {exception.Message}");
                        _plugin.ErrorStatus(exception.Message);
                    }
                }
                finally
                {
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
        }

        private void ClientOnOpen(object sender, EventArgs eventArgs)
        {
            _client?.SendCommand("version", new { version = 2 }, _cancellationTokenSource.Token);
            _client?.SendCommand("new_device", "2E1F407206FF4353B33D724CD1429550", _cancellationTokenSource.Token);

            foreach (object command in _commandsOnReconnect)
            {
                _client?.SendObject(command, _cancellationTokenSource.Token);
            }
        }

        private void ClientOnClose(object sender, EventArgs eventArgs)
        {
            _plugin.NotConnectedStatus();
        }

        private void ClientOnMessage(object sender, MessageReceivedEventArgs message)
        {
            try
            {
                if (message.MessageType == WebSocketMessageType.Text)
                {
                    var data = message.Data.Array;
                    var json = Encoding.UTF8.GetString(data);
                    HandleJsonResponse(json);
                }
            }
            catch
            {
                //Ignore for now.
            }
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
