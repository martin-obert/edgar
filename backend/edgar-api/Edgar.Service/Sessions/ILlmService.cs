using Edgar.Service.Ollama;

namespace Edgar.Service.Sessions;

public interface ILlmService
{
    Task GenerateResponseAsync(ChatMessageBag chatMessages,
        MessageOptions messageOptions,
        OllamaModelDefinition modelConfiguration,
        Action<OllamaResponseChunk> onChunkReceived,
        CancellationToken cancellationToken);
}