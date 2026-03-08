using Edgar.Service.Ollama;

namespace Edgar.Service.Sessions;

public class ChatRepository : IChatRepository
{
    private readonly Dictionary<Guid, List<OllamaChatMessage>> _chats = new();

    public List<OllamaChatMessage> GetMessagesForSession(Guid sessionId)
    {
        if (_chats.TryGetValue(sessionId, out var chat))
        {
            return chat;
        }

        var result = new List<OllamaChatMessage>();
        _chats.Add(sessionId, result);
        return result;
    }
}