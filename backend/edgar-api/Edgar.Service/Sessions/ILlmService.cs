using Edgar.Service.Ollama;

namespace Edgar.Service.Sessions;

public interface ILlmService
{
    Task GenerateResponseAsync(IEnumerable<OllamaChatMessage> chatMessages,
        MessageOptions messageOptions,
        OllamaModelDefinition modelConfiguration,
        Action<OllamaResponseChunk> onChunkReceived,
        CancellationToken cancellationToken);
}