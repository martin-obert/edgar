using System.Net.WebSockets;

namespace Edgar.Service.Sessions;

public class WebSocketMessageStreamWriter(WebSocket webSocket, ILogger<WebSocketMessageStreamWriter> logger)
    : IMessageStreamWriter
{
    public async Task WriteAsync(MessageEnvelope message, CancellationToken cancellationToken = default)
    {
        if (webSocket.State != WebSocketState.Open)
        {
            logger.LogWarning("WebSocket is not open, ignoring message");
            return;
        }

        var bytes = MessageFormatter.SerializeToBytes(message);
        await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true, cancellationToken);
    }
}