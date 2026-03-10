using System.Net.WebSockets;
using System.Text;
using Edgar.Service.Ollama;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Edgar.Service.Sessions;

public class SessionManager : IDisposable
{
    private readonly ILogger _logger;
    private readonly ILlmService _llmService;
    private readonly IChatRepository _chatRepository;
    private readonly Session _session;

    public SessionManager(Session session, IServiceProvider provider)
    {
        _session = session;
        var combine = Path.Combine(Directory.GetCurrentDirectory(), "logs", "sessions", session.Id.ToString(),
            "trace.log");
        _logger = new LoggerConfiguration()
            .WriteTo.Logger(lc => lc
                .WriteTo.File(combine,
                    shared: true,  // allows other processes to read while Serilog writes
                    flushToDiskInterval: TimeSpan.FromSeconds(2))).WriteTo
            .Console(theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
            .CreateLogger();
        _llmService = provider.GetRequiredService<ILlmService>();
        _chatRepository = provider.GetRequiredService<IChatRepository>();
    }

    public void Dispose()
    {
    }

    public async Task LoopAsync(WebSocket webSocket, CancellationToken cancellationToken = default)
    {
        _logger.Information("Session {SessionId} started, looping", _session.Id);
        await ReceiveLoop(webSocket, cancellationToken);
    }

    private async Task ReceiveLoop(WebSocket webSocket, CancellationToken cancellationToken)
    {
        _logger.Information("Fetching messages for session {SessionId}", _session.Id);
        var chat = _chatRepository.GetMessagesForSession(_session.Id);

        var messageHandler = new MessageHandler(
            new WebSocketMessageStreamWriter(webSocket, _logger), _llmService,
            _session.ModelConfiguration ?? OllamaDefinitions.DefaultModel, chat, _logger);
        var buffer = new byte[1024 * 4];
        using var memoryStream = new MemoryStream();
        while (webSocket.State == WebSocketState.Open)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.Information("Cancellation requested");
                break;
            }

            try
            {
                var segment = new ArraySegment<byte>(buffer);
                _logger.Information("Waiting for message");
                var result = await webSocket.ReceiveAsync(segment, cancellationToken);

                _logger.Information("Message received: EOM: {EndOfMessage}, Type: {MessageType}, Count: {Count}",
                    result.EndOfMessage, result.MessageType, result.Count);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal closure", cancellationToken);
                    _logger.Information("Closing connection");
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
                    _logger.Information("Received message: {Message}", json);
                    var receivedMessage = MessageFormatter.Deserialize(json);

                    if (receivedMessage is null) throw new Exception("Received message is null");

                    messageHandler.HandleMessage(receivedMessage, cancellationToken);
                }
            }
            catch (Exception e)
            {
                if(webSocket.State == WebSocketState.Open)
                    await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Internal server error", cancellationToken);
                _logger.Error(e, "Error in receive loop");
            }
        }
    }
}