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
    
    
    public async Task<string[]> GetChatLog(Guid sessionId)
    {
        var file = Path.Combine(Directory.GetCurrentDirectory(), "logs", "sessions", sessionId.ToString(),
            "chat.log");
        if (!File.Exists(file))
            return [];
        await using var stream = new FileStream(
            file, 
            FileMode.Open, 
            FileAccess.Read, 
            FileShare.ReadWrite  // tolerate the writer holding the file open
        );
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        return content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        
    }
}