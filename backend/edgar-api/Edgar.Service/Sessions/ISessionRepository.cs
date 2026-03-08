namespace Edgar.Service.Sessions;

public interface ISessionRepository
{
    Task<Session?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken);
    Task DeleteAsync(Guid sessionId, CancellationToken cancellationToken);

    Task InsertOrUpdateAsync(Session session, CancellationToken cancellationToken);
}

public class InMemorySessionRepository : ISessionRepository
{
    private readonly Dictionary<Guid, Session> _sessions = new();


    public Task<Session?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_sessions.TryGetValue(sessionId, out var session) ? session : null);
    }

    public Task DeleteAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        _sessions.Remove(sessionId);
        return Task.CompletedTask;
    }

    public Task InsertOrUpdateAsync(Session session, CancellationToken cancellationToken)
    {
        _sessions[session.Id] = session;

        return Task.CompletedTask;
    }
}