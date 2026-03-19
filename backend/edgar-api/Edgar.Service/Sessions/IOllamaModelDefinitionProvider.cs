using Edgar.Service.Ollama;

namespace Edgar.Service.Sessions;

public interface IOllamaModelDefinitionProvider
{
    Task<OllamaModelDefinition> GetDefaultModelDefinitionAsync(CancellationToken cancellationToken = default);
    Task<OllamaModelDefinition> GetSessionModelDefinitionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}