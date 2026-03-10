using System.Text;
using Edgar.Service.Ollama;
using ILogger = Serilog.ILogger;

namespace Edgar.Service.Sessions;

public class MessageHandler(
    IMessageStreamWriter messageStreamWriter,
    ILlmService llmService,
    OllamaModelDefinition modelConfiguration,
    ChatMessageBag messages,
    ILogger logger)
{
    private class PromptProgress
    {
        public List<OllamaToolCallRequest> ToolCalls { get; } = new();
        public StringBuilder Content = new();
        public StringBuilder Thinking = new();
        public bool ChunksComplete = false;

        public bool IsComplete => ChunksComplete && ToolCalls.All(t => t.IsResolved) &&
                                  (SubPrompt is null || SubPrompt.IsComplete);

        public required string PromptId { get; init; }

        public PromptProgress? SubPrompt { get; set; }

        public OllamaChatMessage ToChatMessage()
        {
            return new OllamaChatMessage
            {
                Content = Content.ToString(),
                ToolCalls = ToolCalls.Cast<OllamaToolCall>().ToArray(),
                Thinking = Thinking.ToString(),
                Role = KnownRoles.Assistant,
                CreatedAt = DateTime.UtcNow,
            };
        }
    }


    private PromptProgress? _promptProgress;

    /// <summary>
    /// Non-blocking message handler allows continuous processing of messages
    /// </summary>
    /// <param name="receivedMessage"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="Exception"></exception>
    public void HandleMessage(MessageEnvelope receivedMessage, CancellationToken cancellationToken = default)
    {
        Task.Run(async () =>
        {
            try
            {
                logger.Information("Handling message for role: {Role}", receivedMessage.Role);
                switch (receivedMessage.Role)
                {
                    case KnownRoles.User:
                        await HandleUserPrompt(receivedMessage, cancellationToken);
                        break;
                    case KnownRoles.Tool:
                        await HandleToolRoleMessage(receivedMessage, cancellationToken);
                        break;
                    default:
                        throw new Exception($"Unexpected role: {receivedMessage.Role}");
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error handling message for role: {Role}", receivedMessage.Role);
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleToolRoleMessage(MessageEnvelope receivedMessage,
        CancellationToken cancellationToken = default)
    {
        if (_promptProgress is null)
        {
            logger.Warning("Tool call received, but no prompt is active");
            return;
        }

        if (receivedMessage.PromptId != _promptProgress.PromptId)
        {
            logger.Warning("Tool call received, but prompt id does not match");
            return;
        }

        if (string.IsNullOrWhiteSpace(receivedMessage.ToolCallId))
        {
            logger.Warning("Tool call received, but tool call id is null or empty");
            return;
        }

        var pendingToolRequest = _promptProgress.ToolCalls.FirstOrDefault(t => t.Id == receivedMessage.ToolCallId);

        if (pendingToolRequest is null)
        {
            logger.Warning("Tool call {ToolCallId} received, but no pending tool call found",
                receivedMessage.ToolCallId);
            return;
        }

        if (pendingToolRequest.IsResolved)
        {
            logger.Warning("Tool call {ToolCallId} received, but it is already resolved",
                receivedMessage.ToolCallId);
        }

        pendingToolRequest.IsResolved = true;

        messages.Add(new OllamaChatMessage
        {
            Role = KnownRoles.Tool,
            Content = receivedMessage.Body,
            ToolName = pendingToolRequest.Function.Name,
            CreatedAt = DateTime.UtcNow,
        });
        await GenerateAndProcessResponseAsync(receivedMessage, cancellationToken);
    }

    private async Task HandleUserPrompt(MessageEnvelope receivedMessage, CancellationToken cancellationToken = default)
    {
        if (receivedMessage.Body == null)
            return;

        messages.Add(new OllamaChatMessage
        {
            Role = KnownRoles.User,
            Content = receivedMessage.Body,
            CreatedAt = DateTime.UtcNow,
        });

        await GenerateAndProcessResponseAsync(receivedMessage, cancellationToken);
    }

    private async Task GenerateAndProcessResponseAsync(MessageEnvelope receivedMessage,
        CancellationToken cancellationToken)
    {
        //TODO: wrap this into separate class
        var localProgress = UpdateMessageContext(receivedMessage);

        if (localProgress is null)
        {
            logger.Warning("Prompt progress is null, ignoring message");
            return;
        }

        var chunkId = 0;

        await llmService.GenerateResponseAsync(messages, OnChunkReceived, modelConfiguration,
            cancellationToken);

        var delayBudget = TimeSpan.FromSeconds(10);

        while (!localProgress.IsComplete && delayBudget > TimeSpan.Zero)
        {
            delayBudget -= TimeSpan.FromMilliseconds(100);
            await Task.Delay(100, cancellationToken);
        }

        if (localProgress.ToolCalls.Any())
        {
            await llmService.GenerateResponseAsync(messages, OnChunkReceived, modelConfiguration,
                cancellationToken);
        }

        if (localProgress.IsComplete)
        {
            messages.Add(localProgress.ToChatMessage());
        }

        messageStreamWriter.WriteAsync(MessageFactory.SignalPromptComplete(localProgress.PromptId), cancellationToken);
        _promptProgress = null;

        return;

        void OnChunkReceived(OllamaResponseChunk obj)
        {
            chunkId++;
            if (obj.Done)
                localProgress.ChunksComplete = true;

            if (obj.Message.ToolCalls is not null)
                foreach (var ollamaToolCall in obj.Message.ToolCalls)
                {
                    localProgress.ToolCalls.Add(new OllamaToolCallRequest
                    {
                        Function = ollamaToolCall.Function,
                        Id = ollamaToolCall.Id,
                        // Type = ollamaToolCall.Type,
                    });
                   messageStreamWriter.WriteAsync(MessageFactory.ToolCall(ollamaToolCall.Function, localProgress.PromptId, ollamaToolCall.Id), cancellationToken);
                }

            if (!string.IsNullOrWhiteSpace(obj.Message.Thinking))
            {
                localProgress.Thinking.Append(obj.Message.Thinking);
                messageStreamWriter.WriteAsync(MessageFactory.ThinkingChunk(obj.Message.Thinking,
                    localProgress.PromptId), cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(obj.Message.Content))
            {
                if (obj.Message.Role != KnownRoles.Assistant)
                    throw new Exception($"Unexpected role: {obj.Message.Role}");

                localProgress.Content.Append(obj.Message.Content);
                messageStreamWriter.WriteAsync(MessageFactory.ChunkResponse(obj.Message.Content, localProgress.PromptId,
                    chunkId.ToString()), cancellationToken);
            }
        }
    }

    private PromptProgress? UpdateMessageContext(MessageEnvelope receivedMessage)
    {
        if (_promptProgress == null)
        {
            if (receivedMessage.Role == KnownRoles.User)
            {
                _promptProgress = new PromptProgress
                {
                    PromptId = receivedMessage.PromptId ?? Guid.NewGuid().ToString(),
                };
                return _promptProgress;
            }

            logger.Warning("Tool call received, but no prompt is active");
            return null;
        }

        if (receivedMessage.Role == KnownRoles.Tool)
        {
            var subPrompt = new PromptProgress
            {
                PromptId = _promptProgress.PromptId,
            };
            _promptProgress.SubPrompt = subPrompt;
            return subPrompt;
        }

        return _promptProgress;
    }
}