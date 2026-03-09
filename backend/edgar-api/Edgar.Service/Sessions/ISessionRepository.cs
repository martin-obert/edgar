using System.Collections;
using Edgar.Service.Ollama;

namespace Edgar.Service.Sessions;

public interface ISessionRepository
{
    Task<Session?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken);
    Task DeleteAsync(Guid sessionId, CancellationToken cancellationToken);

    Task InsertOrUpdateAsync(Session session, CancellationToken cancellationToken);
    Task<Session[]> ListSessionsAsync();

    Task UpdateSessionConfigurationAsync(Guid sessionId, OllamaModelDefinition cancellationToken,
        CancellationToken token);
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

    public Task<Session[]> ListSessionsAsync()
    {
        return Task.FromResult(_sessions.Values.ToArray());
    }

    public Task UpdateSessionConfigurationAsync(Guid sessionId, OllamaModelDefinition cancellationToken,
        CancellationToken token)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            _sessions[sessionId] = new Session
            {
                CreatedAt = DateTime.UtcNow,
                Id = sessionId,
                ModelConfiguration = cancellationToken,
                State = SessionState.Created
            };
            return Task.CompletedTask;
        }

        _sessions[sessionId].ModelConfiguration = cancellationToken;
        return Task.CompletedTask;
    }
}