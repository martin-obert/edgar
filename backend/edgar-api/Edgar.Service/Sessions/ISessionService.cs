using Edgar.Service.Ollama;

namespace Edgar.Service.Sessions;

public interface ISessionService
{
    Task<Session> CreateSessionAsync(CancellationToken cancellationToken);
    Task<Session?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken);
    Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken);
    Task SetSessionStateAsync(Guid sessionId, SessionState state, CancellationToken cancellationToken);
    Task UpdateSessionDefinitionAsync(Guid sessionId, OllamaModelDefinition configuration, CancellationToken token);
}

public class SessionService(ISessionRepository sessionRepository) : ISessionService
{
    public async Task<Session> CreateSessionAsync(CancellationToken cancellationToken)
    {
        var session = new Session
        {
            Id = Guid.NewGuid(),
            ModelConfiguration = OllamaDefinitions.DefaultModel,
            CreatedAt = DateTime.UtcNow,
            State = SessionState.Created
        };
        await sessionRepository.InsertOrUpdateAsync(session, cancellationToken);
        return session;
    }

    public Task<Session?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return sessionRepository.GetByIdAsync(sessionId, cancellationToken);
    }

    public Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return sessionRepository.DeleteAsync(sessionId, cancellationToken);
    }

    public async Task SetSessionStateAsync(Guid sessionId, SessionState state, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session != null) session.State = state;
    }

    public Task UpdateSessionDefinitionAsync(Guid sessionId, OllamaModelDefinition configuration, CancellationToken token)
    {
       return sessionRepository.UpdateSessionConfigurationAsync(sessionId, configuration, token);
    }
}