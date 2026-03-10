namespace Edgar.Service.Sessions;

public class ChatRepository : IChatRepository
{
    private readonly Dictionary<Guid,ChatMessageBag> _chats = new();

    public ChatMessageBag GetMessagesForSession(Guid sessionId)
    {
        if (_chats.TryGetValue(sessionId, out var chat))
        {
            return chat;
        }

        var result = new ChatMessageBag(new ChatLog(sessionId));
        _chats.Add(sessionId, result);
        return result;
    }
}