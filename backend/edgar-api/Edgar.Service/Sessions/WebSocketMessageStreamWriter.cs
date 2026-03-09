using System.Net.WebSockets;
using System.Text;
using ILogger = Serilog.ILogger;

namespace Edgar.Service.Sessions;

public class WebSocketMessageStreamWriter(WebSocket webSocket, ILogger logger)
    : IMessageStreamWriter
{
    public void WriteAsync(MessageEnvelope message, CancellationToken cancellationToken = default)
    {
        Task.Run(async () =>
        {
            logger.Information("Sending message to client");
            if (webSocket.State != WebSocketState.Open)
            {
                logger.Warning("WebSocket is not open, ignoring message");
                return;
            }

            var json = MessageFormatter.Serialize(message);
            logger.Information("Sending message to client: {Message}", json);
            var bytes = Encoding.UTF8.GetBytes(json);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true,
                cancellationToken);
            logger.Information("Message sent to client {Id}", message.PromptId);
        }, cancellationToken).ConfigureAwait(false);
    }
}