using Edgar.Service.Ollama;

namespace Edgar.Service.Sessions;

public interface ISessionService
{
    Task<Session> CreateSessionAsync(CancellationToken cancellationToken);
    Task<Session?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken);
    Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken);
}


public class SessionService(ISessionRepository sessionRepository) : ISessionService
{
    public async Task<Session> CreateSessionAsync(CancellationToken cancellationToken)
    {
        var session = new Session
        {
            Id = Guid.NewGuid(),
            ModelConfiguration = OllamaDefinitions.DefaultModel
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
}