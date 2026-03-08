using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Edgar.Service.Ollama;

namespace Edgar.Service.Sessions;

public class SessionManager(Session session, IServiceProvider provider) : IDisposable
{
    private readonly ILogger<SessionManager> _logger = provider.GetRequiredService<ILogger<SessionManager>>();
    private readonly ILlmService _llmService = provider.GetRequiredService<ILlmService>();
    private readonly IChatRepository _chatRepository = provider.GetRequiredService<IChatRepository>();

    private Func<ILogger<MessageHandler>> GetMessageLogger { get; } =
        provider.GetRequiredService<ILogger<MessageHandler>>;

    private Func<ILogger<WebSocketMessageStreamWriter>> GetWebSocketMessageLogger { get; } =
        provider.GetRequiredService<ILogger<WebSocketMessageStreamWriter>>;

    public void Dispose()
    {
    }

    public async Task LoopAsync(WebSocket webSocket, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Session {SessionId} started, looping", session.Id);
        await ReceiveLoop(webSocket, cancellationToken);
    }

    private async Task ReceiveLoop(WebSocket webSocket, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching messages for session {SessionId}", session.Id);
        var chat = _chatRepository.GetMessagesForSession(session.Id);

        var messageHandler = new MessageHandler(
            new WebSocketMessageStreamWriter(webSocket, GetWebSocketMessageLogger()), _llmService,
            session.ModelConfiguration ?? OllamaDefinitions.DefaultModel, chat, GetMessageLogger());
        var buffer = new byte[1024 * 4];
        using var memoryStream = new MemoryStream();
        while (webSocket.State == WebSocketState.Open)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Cancellation requested");
                break;
            }

            var segment = new ArraySegment<byte>(buffer);
            _logger.LogInformation("Waiting for message");
            var result = await webSocket.ReceiveAsync(segment, cancellationToken);

            _logger.LogInformation("Message received: EOM: {EndOfMessage}, Type: {MessageType}, Count: {Count}",
                result.EndOfMessage, result.MessageType, result.Count);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                _logger.LogInformation("Closing connection");
                break;
            }

            memoryStream.Write(buffer, 0, result.Count);

            if (result.EndOfMessage)
            {
                var bytes = memoryStream.ToArray();
                memoryStream.Seek(0, SeekOrigin.Begin);
                memoryStream.Position = 0;
                memoryStream.SetLength(0);

                var json = Encoding.UTF8.GetString(bytes);
                _logger.LogInformation("Received message: {Message}", json);
                var receivedMessage = MessageFormatter.Deserialize(json);

                if (receivedMessage is null) throw new Exception("Received message is null");

                messageHandler.HandleMessage(receivedMessage, cancellationToken);
            }
        }
    }
}