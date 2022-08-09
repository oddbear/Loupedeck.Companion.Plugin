using System;
using System.Net.WebSockets;
using System.Text;
using Loupedeck.CompanionPlugin.Extensions;
using Loupedeck.CompanionPlugin.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WatsonWebsocket;

namespace Loupedeck.CompanionPlugin
{
    public class CompanionPlugin : Plugin
    {
        public override bool UsesApplicationApiOnly => true;
        public override bool HasNoApplication => true;

        internal event EventHandler<ResponseFillImage> FillImageResponse;

        public WatsonWsClient Client;

        public CompanionPlugin()
        {
            //var source = new CancellationTokenSource();
            //var cancellationToken = source.Token;

            var uri = new Uri("ws://127.0.0.1:28492");
            Client = new WatsonWsClient(uri);
            Client.MessageReceived += HandleResponse;
        }
        
        public override void Load()
        {
            this.LoadPluginIcons();

            Client.Start();
            Client.SendCommand("version", new { version = 2 });
            Client.SendCommand("new_device", "2E1F407206FF4353B33D724CD1429550");
        }

        public override void Unload()
        {
            Client.Dispose();
        }
        
        public override void RunCommand(string commandName, string parameter)
        {
        }

        public override void ApplyAdjustment(string adjustmentName, string parameter, int diff)
        {
        }
        
        private void HandleResponse(object sender, MessageReceivedEventArgs message)
        {
            if (message.Data.Array is null)
                return;

            try
            {
                switch (message.MessageType)
                {
                    case WebSocketMessageType.Text:
                        var text = Encoding.UTF8.GetString(message.Data.Array);
                        HandleJsonResponse(text);
                        break;
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
