using Edgar.Service.Ollama;

namespace Edgar.Service.Sessions;

public interface IChatLog : IDisposable
{
    void LogMessage(OllamaChatMessage message);
}