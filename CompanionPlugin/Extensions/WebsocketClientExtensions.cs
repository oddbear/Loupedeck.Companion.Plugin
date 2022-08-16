using System.Threading;
using Newtonsoft.Json;
using WebSocketSharp;

namespace Loupedeck.CompanionPlugin.Extensions
{
    public static class WebSocketClientExtensions
    {
        public static void SendCommand(this WebSocket client, string command, object obj, CancellationToken cancellationToken = default)
        {
            client.SendObject(new { command, arguments = obj }, cancellationToken);
        }

        public static void SendObject(this WebSocket client, object obj, CancellationToken cancellationToken = default)
        {
            try
            {

                if (cancellationToken.IsCancellationRequested)
                    return;

                if (client is null)
                    return;

                if (!client.IsConnected())
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
