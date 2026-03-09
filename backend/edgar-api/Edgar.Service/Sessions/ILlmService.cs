using Edgar.Service.Ollama;

namespace Edgar.Service.Sessions;

public interface ILlmService
{
    Task GenerateResponseAsync(ChatMessageBag chatMessages,
        Action<OllamaResponseChunk> onChunkReceived,
        OllamaModelDefinition modelConfiguration,
        CancellationToken cancellationToken);
}