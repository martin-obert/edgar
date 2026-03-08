using Edgar.Service.Ollama;

namespace Edgar.Service.Sessions;

public interface ILlmService
{
    Task GenerateResponseAsync(IEnumerable<OllamaChatMessage> chatMessages,
        Action<OllamaResponseChunk> onChunkReceived,
        OllamaModelDefinition modelConfiguration,
        CancellationToken cancellationToken);
}