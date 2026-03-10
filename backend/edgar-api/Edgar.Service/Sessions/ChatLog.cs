using Edgar.Service.Ollama;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Edgar.Service.Sessions;

public sealed class ChatLog : IChatLog
{
    private readonly ILogger _logger;

    public ChatLog(Guid sessionId)
    {
        var combine = Path.Combine(Directory.GetCurrentDirectory(), "logs", "sessions", sessionId.ToString(),
            "chat.log");
        _logger = new LoggerConfiguration()
            .WriteTo.Logger(lc => lc
                .WriteTo.File(combine,
                    outputTemplate: "{Message:lj}{NewLine}",
                    flushToDiskInterval: TimeSpan.FromSeconds(2)))
            .CreateLogger();
        ;
    }

    public void LogMessage(OllamaChatMessage message)
    {
        _logger.Information("{@Message}", message);
    }

    public void Dispose()
    {
    }
}