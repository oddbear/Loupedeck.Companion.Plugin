using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Loupedeck.CompanionPlugin.Extensions;
using Loupedeck.CompanionPlugin.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

namespace Loupedeck.CompanionPlugin
{
    public class CompanionPlugin : Plugin
    {
        public override bool UsesApplicationApiOnly => true;
        public override bool HasNoApplication => true;

        internal event EventHandler<ResponseFillImage> FillImageResponse;

        public WebSocket Client;
        private CancellationTokenSource _cancellationTokenSource;

        public CompanionPlugin()
        {
            Client = new WebSocket("ws://127.0.0.1:28492");
            Client.Log.Output = Logging;
            Client.OnOpen += ClientOnOpen;
            Client.OnClose += ClientOnClose;
            Client.OnMessage += ClientOnMessage;
        }

        private void Logging(LogData logData, string _)
        {
#if DEBUG
            Trace.WriteLine(logData.Message);
#endif
        }

        public override void Load()
        {
            this.LoadPluginIcons();

            _cancellationTokenSource = new CancellationTokenSource();

            _ = Task.Run(Reconnect);
        }

        public override void Unload()
        {
            _cancellationTokenSource.Cancel();
            ((IDisposable)Client).Dispose();
        }
        
        public override void RunCommand(string commandName, string parameter)
        {
        }

        public override void ApplyAdjustment(string adjustmentName, string parameter, int diff)
        {
        }

        private void Reconnect()
        {
            var token = _cancellationTokenSource.Token;
            while (Client.ReadyState != WebSocketState.Open)
            {
                if (token.IsCancellationRequested)
                    return;

                base.OnPluginStatusChanged(Loupedeck.PluginStatus.Warning, "Disconnected", "https://github.com/oddbear/Loupedeck.Companion.Plugin", "Companion Repository");

                //This is kind of a hack, but if we don't do this, it will fail after 10 retries.
                typeof(WebSocket)
                    .GetField("_retryCountForConnect", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(Client, 1);

                Client.Connect();
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
            base.OnPluginStatusChanged(Loupedeck.PluginStatus.Normal, "Connected", "https://github.com/oddbear/Loupedeck.Companion.Plugin", "Companion Repository");
        }

        private void ClientOnOpen(object sender, EventArgs eventArgs)
        {
            var token = _cancellationTokenSource.Token;
            Client.SendCommand("version", new { version = 2 }, token);
            Client.SendCommand("new_device", "2E1F407206FF4353B33D724CD1429550", token);
        }

        private void ClientOnClose(object sender, CloseEventArgs closeEventArgs)
        {
            Reconnect();
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
                if(arguments is null)
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

        private void LoadPluginIcons()
        {
            this.Info.Icon16x16 = EmbeddedResources.ReadImage("Loupedeck.CompanionPlugin.Resources.Icons.Icon-16.png");
            this.Info.Icon32x32 = EmbeddedResources.ReadImage("Loupedeck.CompanionPlugin.Resources.Icons.Icon-32.png");
            this.Info.Icon48x48 = EmbeddedResources.ReadImage("Loupedeck.CompanionPlugin.Resources.Icons.Icon-48.png");
            this.Info.Icon256x256 = EmbeddedResources.ReadImage("Loupedeck.CompanionPlugin.Resources.Icons.Icon-256.png");
        }
    }
}
