namespace Edgar.Service.Sessions;

public interface IChatRepository
{
    ChatMessageBag GetMessagesForSession(Guid sessionId);
    Task<string[]> GetChatLog(Guid sessionId);
}

public interface ISessionLogRepository
{
    Task<string[]> GetTraceLog(Guid sessionId);
}

public class SessionLogRepository : ISessionLogRepository
{
    public async Task<string[]> GetTraceLog(Guid sessionId)
    {
        var file = Path.Combine(Directory.GetCurrentDirectory(), "logs", "sessions", sessionId.ToString(),
            "trace.log");
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

