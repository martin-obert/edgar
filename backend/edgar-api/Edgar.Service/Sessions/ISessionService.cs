using Edgar.Service.Ollama;

namespace Edgar.Service.Sessions;

public interface ISessionService
{
    Task<Session?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken);
    Task UpdateSessionStateAsync(Guid sessionId, SessionState state, CancellationToken cancellationToken);
    Task CreateOrUpdateSession(Guid sessionId, OllamaModelDefinition configuration, CancellationToken token);
}