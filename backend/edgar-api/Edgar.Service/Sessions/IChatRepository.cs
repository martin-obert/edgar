using Edgar.Service.Ollama;

namespace Edgar.Service.Sessions;

public interface IChatRepository
{
    List<OllamaChatMessage> GetMessagesForSession(Guid sessionId);
}