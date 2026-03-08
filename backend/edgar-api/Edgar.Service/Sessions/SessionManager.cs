using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Edgar.Service.Sessions;

public class SessionManager(Session session, ILogger<SessionManager> logger) : IDisposable
{
    public void Dispose()
    {
    }

    public async Task LoopAsync(WebSocket webSocket, CancellationToken cancellationToken = default)
    {
        await ReceiveLoop(webSocket, cancellationToken);
    }

    private void DispatchMessage(WebSocket webSocket, string message, CancellationToken cancellationToken)
    {
        Task.Run(async () => { webSocket.SendAsync(, WebSocketMessageType.Binary, true, cancellationToken) },
            cancellationToken).ConfigureAwait(false);
    }


    private async Task ReceiveLoop(WebSocket webSocket, CancellationToken cancellationToken)
    {
        var messageHandler = new MessageHandler();
        var buffer = new byte[1024 * 4];
        using var memoryStream = new MemoryStream();
        while (webSocket.State == WebSocketState.Open)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation("Cancellation requested");
                break;
            }

            var segment = new ArraySegment<byte>(buffer);
            var result = await webSocket.ReceiveAsync(segment, cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                logger.LogInformation("Closing connection");
                break;
            }

            memoryStream.Write(buffer, 0, result.Count);

            if (result.EndOfMessage)
            {
                var bytes = memoryStream.ToArray();
                var json = Encoding.UTF8.GetString(bytes);
                memoryStream.Seek(0, SeekOrigin.Begin);
                memoryStream.Position = 0;
                memoryStream.SetLength(0);
                
                var receivedMessage = JsonSerializer.Deserialize<MessageEnvelope>(json);

                if(receivedMessage is null) throw new Exception("Received message is null");
                
                messageHandler.HandleMessage(receivedMessage, cancellationToken);
            }
        }
    }
}