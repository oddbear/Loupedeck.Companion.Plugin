using System.Net.WebSockets;
using System.Threading;
using Newtonsoft.Json;
using WatsonWebsocket;

namespace Loupedeck.CompanionPlugin.Extensions
{
    public static class WebSocketClientExtensions
    {
        public static void SendCommand(this WatsonWsClient client, string command, object obj, CancellationToken cancellationToken = default)
        {
            client.SendObject(new { command, arguments = obj }, cancellationToken);
        }

        public static void SendObject(this WatsonWsClient client, object obj, CancellationToken cancellationToken = default)
        {
            try
            {

                if (cancellationToken.IsCancellationRequested)
                    return;

                if (client is null)
                    return;

                if (!client.Connected)
                    return;

                var json = JsonConvert.SerializeObject(obj);
                client.SendAsync(json, WebSocketMessageType.Text, cancellationToken)
                    .GetAwaiter()
                    .GetResult();
            }
            catch
            {
                //
            }
        }
    }
}
