using System.Threading;
using Newtonsoft.Json;
using Websocket.Client;

namespace Loupedeck.CompanionPlugin.Extensions
{
    public static class WebSocketClientExtensions
    {
        public static void SendCommand(this WebsocketClient client, string command, object obj, CancellationToken cancellationToken = default)
        {
            client.SendObject(new { command, arguments = obj }, cancellationToken);
        }

        public static void SendObject(this WebsocketClient client, object obj, CancellationToken cancellationToken = default)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                if (client is null)
                    return;

                if (!client.IsRunning)
                    return;

                var json = JsonConvert.SerializeObject(obj);
                client.Send(json);
            }
            catch
            {
                //
            }
        }
    }
}
