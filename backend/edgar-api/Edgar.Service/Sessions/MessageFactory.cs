using Edgar.Service.Ollama;

namespace Edgar.Service.Sessions;

public static class MessageFactory
{
    public static MessageEnvelope SignalPromptComplete(string promptId)
    {
        return new MessageEnvelope
        {
            Headers =
            [
                new MessageHeader { Name = KnownHeaders.PromptId, Value = promptId },
                new MessageHeader { Name = KnownHeaders.Role, Value = KnownRoles.Assistant },
                new MessageHeader { Name = KnownHeaders.Signal, Value = SignalTypes.RequestComplete }
            ],
        };
    }

    public static MessageEnvelope ToolCall(OllamaToolCallFunction toolCall, string promptId, string toolCallId)
    {
        return new MessageEnvelope
        {
            Body = MessageFormatter.Serialize(toolCall),
            Headers =
            [
                new MessageHeader { Name = KnownHeaders.PromptId, Value = promptId },
                new MessageHeader { Name = KnownHeaders.ToolCallId, Value = toolCallId },
                new MessageHeader { Name = KnownHeaders.Role, Value = KnownRoles.Tool }
            ]
        };
    }
    
    public static MessageEnvelope ChunkResponse(string response, string promptId, string chunkId)
    {
        return new MessageEnvelope
        {
            Body = response,
            Headers =
            [
                new MessageHeader { Name = KnownHeaders.PromptId, Value = promptId },
                new MessageHeader { Name = KnownHeaders.MessageChunkId, Value = chunkId },
                new MessageHeader { Name = KnownHeaders.Role, Value = KnownRoles.Assistant }
            ]
        };
    }

    public static MessageEnvelope ThinkingChunk(string thinking, string promptId)
    {
        return new MessageEnvelope
        {
            Body = thinking,
            Headers =
            [
                new MessageHeader { Name = KnownHeaders.PromptId, Value = promptId },
                new MessageHeader { Name = KnownHeaders.Role, Value = KnownRoles.Assistant }
            ]
        };
    }
}