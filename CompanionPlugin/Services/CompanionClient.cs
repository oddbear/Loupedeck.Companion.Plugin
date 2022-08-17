using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Threading.Channels;
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

        private readonly Channel<object> _channel;

        public WebSocket Client;

        public bool Connected => Client?.IsConnected() ?? false;

        private readonly List<object> _commandsOnReconnect = new List<object>();

        public CompanionClient(CompanionPlugin plugin)
        {
            _channel = Channel.CreateUnbounded<object>();
            _plugin = plugin;
            Client = CreateClient();
            _thread = new Thread(Reconnect);
            ChannelReader();
        }

        private async void ChannelReader()
        {
            var token = _cancellationTokenSource.Token;
            var channelReader = _channel.Reader;

            while (await channelReader.WaitToReadAsync(token))
                while (channelReader.TryRead(out object command))
                    Client.SendObject(command, token);
        }

        public void OnConnectCommand(object obj)
        {
            _commandsOnReconnect.Add(obj);
            if (Client.IsConnected())
            {
                _channel?.Writer.TryWrite(obj);
            }
        }

        private static object _locker = new object();

        public void SendCommand(string command, object obj)
        {
            if (!Connected)
                return;

            //TODO: Problem with adjustments (more than one command is queued).
            _channel?.Writer.TryWrite(new { command, arguments = obj });
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
            var token = _cancellationTokenSource.Token;
            while (Client.ReadyState != WebSocketState.Open)
            {
                if (token.IsCancellationRequested)
                    return;

                try
                {
                    Client.Connect();

                    if (!Client.IsConnected())
                    {
                        _plugin.NotConnectedStatus();
                    }

                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"{ex.GetType().Name}: {ex.Message}");

                    IDisposable oldClient = Client;
                    Client = CreateClient();
                    oldClient.Dispose();
                }
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }

            _plugin.ConnectedStatus();
        }
        private void ClientOnOpen(object sender, EventArgs eventArgs)
        {
            var token = _cancellationTokenSource.Token;
            _channel?.Writer.TryWrite(new { command = "version", arguments = new { version = 2 } });
            _channel?.Writer.TryWrite(new { command = "new_device", arguments = "2E1F407206FF4353B33D724CD1429550" });

            foreach (var command in _commandsOnReconnect)
            {
                _channel?.Writer.TryWrite(command);
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
            ((IDisposable) Client)?.Dispose();
        }
    }
}
