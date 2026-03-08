using System.Net.WebSockets;
using System.Text;

namespace Edgar.Service.Sessions;

public class WebSocketMessageStreamWriter(WebSocket webSocket, ILogger<WebSocketMessageStreamWriter> logger)
    : IMessageStreamWriter
{
    public void WriteAsync(MessageEnvelope message, CancellationToken cancellationToken = default)
    {
        Task.Run(async () =>
        {
            logger.LogInformation("Sending message to client");
            if (webSocket.State != WebSocketState.Open)
            {
                logger.LogWarning("WebSocket is not open, ignoring message");
                return;
            }

            var json = MessageFormatter.Serialize(message);
            logger.LogInformation("Sending message to client: {Message}", json);
            var bytes = Encoding.UTF8.GetBytes(json);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true,
                cancellationToken);
            logger.LogInformation("Message sent to client {Id}", message.PromptId);
        }, cancellationToken).ConfigureAwait(false);
    }
}