using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Loupedeck.CompanionPlugin.Extensions;
using Loupedeck.CompanionPlugin.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

namespace Loupedeck.CompanionPlugin.Services
{
    public class CompanionClient : IDisposable
    {
        private readonly CompanionPlugin _plugin;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        internal event EventHandler<ResponseFillImage> FillImageResponse;

        private Thread _thread;

        private WebSocket _client;

        public bool Connected => _client?.ReadyState == WebSocketState.Open;

        private readonly List<object> _commandsOnReconnect = new List<object>();

        public CompanionClient(CompanionPlugin plugin)
        {
            _plugin = plugin;
            _client = CreateClient();
            _thread = new Thread(Reconnect);
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
            _client.SendCommand(command, obj);
        }

        public void Start()
        {
            _thread.Start();
        }

        private WebSocket CreateClient()
        {
            var client = new WebSocket("ws://127.0.0.1:28492");
            client.Log.Output = Logging;
            //client.Log.EnableTraces();
            client.OnOpen += ClientOnOpen;
            client.OnClose += ClientOnClose;
            client.OnMessage += ClientOnMessage;

            return client;
        }

        private void Logging(LogData logData, string _)
        {
#if DEBUG
            Trace.WriteLine(logData.Message);
#endif
        }

        private void Reconnect()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    if (Connected)
                        continue;

                    _client.Connect();

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

                    IDisposable oldClient = _client;
                    _client = CreateClient();
                    oldClient.Dispose();
                }
                finally
                {
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
        }

        private void ClientOnOpen(object sender, EventArgs eventArgs)
        {
            var token = _cancellationTokenSource.Token;
            _client.SendCommand("version", new { version = 2 }, token);
            _client.SendCommand("new_device", "2E1F407206FF4353B33D724CD1429550", token);

            foreach (object command in _commandsOnReconnect)
            {
                _client.SendObject(command, token);
            }
        }

        private void ClientOnClose(object sender, CloseEventArgs closeEventArgs)
        {
            _plugin.NotConnectedStatus();
        }

        private void ClientOnMessage(object sender, MessageEventArgs message)
        {
            if (message.Data is null)
                return;

            try
            {
                if (message.IsText)
                    HandleJsonResponse(message.Data);
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
            ((IDisposable) _client)?.Dispose();
        }
    }
}
