using System.Text;
using Edgar.Service.Ollama;
using ILogger = Serilog.ILogger;

namespace Edgar.Service.Sessions;

public class PromptProgress
{
    public required string PromptId { get; init; }

    public List<OllamaToolCallRequest> ToolCallRequests { get; } = [];
}

public class MessageHandler(
    IMessageStreamWriter messageStreamWriter,
    ILlmService llmService,
    ChatMessageBag messages,
    ILogger logger)
{
    /// <summary>
    /// Non-blocking message handler allows continuous processing of messages
    /// </summary>
    /// <param name="receivedMessage"></param>
    /// <param name="modelConfiguration"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="Exception"></exception>
    public void HandleMessage(MessageEnvelope receivedMessage,
        PromptProgress promptProgress,
        OllamaModelDefinition modelConfiguration,
        CancellationToken cancellationToken = default)
    {
        Task.Run(async () =>
        {
            try
            {
                logger.Information("Handling message for role: {Role}", receivedMessage.Role);
                switch (receivedMessage.Role)
                {
                    case KnownRoles.User:
                        await HandleUserPrompt(promptProgress, receivedMessage, modelConfiguration, cancellationToken);
                        break;
                    case KnownRoles.Tool:
                        await HandleToolResponse(receivedMessage, promptProgress);
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

    private Task HandleToolResponse(MessageEnvelope receivedMessage, PromptProgress promptProgress)
    {
        if (receivedMessage.PromptId != promptProgress.PromptId)
        {
            logger.Warning("Tool call received, but prompt id does not match");
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(receivedMessage.ToolCallId))
        {
            logger.Warning("Tool call received, but tool call id is null or empty");
            return Task.CompletedTask;
        }

        var pendingToolRequest =
            promptProgress.ToolCallRequests.FirstOrDefault(t => t.Id == receivedMessage.ToolCallId);

        if (pendingToolRequest is null)
        {
            logger.Warning("Tool call {ToolCallId} received, but no pending tool call found",
                receivedMessage.ToolCallId);
            return Task.CompletedTask;
        }

        if (pendingToolRequest.IsResolved)
        {
            logger.Warning("Tool call {ToolCallId} received, but it is already resolved",
                receivedMessage.ToolCallId);
        }

        pendingToolRequest.Resolve(receivedMessage.Body);
        ;

        return Task.CompletedTask;
    }

    private async Task HandleUserPrompt(PromptProgress promptProgress, MessageEnvelope receivedMessage,
        OllamaModelDefinition modelConfiguration,
        CancellationToken cancellationToken = default)
    {
        if (receivedMessage.Body == null)
            return;

        messages.Add(new OllamaChatMessage
        {
            Role = KnownRoles.User,
            Content = receivedMessage.Body,
            CreatedAt = DateTime.UtcNow
        });
        await GenerateAndProcessResponseAsync(promptProgress, modelConfiguration, true, cancellationToken);
    }

    private async Task GenerateAndProcessResponseAsync(PromptProgress globalProgress,
        OllamaModelDefinition modelConfiguration,
        bool isRoot, CancellationToken cancellationToken)
    {
        var chunkId = 0;
        var chunksComplete = false;
        var messageOptions = new MessageOptions
        {
            Stream = true,
        };
        StringBuilder content = new();
        StringBuilder thinking = new();
        await llmService.GenerateResponseAsync(messages.Messages, messageOptions, modelConfiguration,
            OnChunkReceived,
            cancellationToken);

        while (!chunksComplete && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(100, cancellationToken);
        }

        // Add an assistant original response with tool call requests
        var ollamaChatMessage = GetAssistantResponse();
        messages.Add(ollamaChatMessage);

        
        while (!globalProgress.ToolCallRequests.All(t => t.IsResolved && !cancellationToken.IsCancellationRequested))
        {
            await Task.Delay(100, cancellationToken);
        }

        // If we got any tool calls, add them to the chat log
        if (globalProgress.ToolCallRequests.Count > 0)
        {
            messages.AddMany(GetToolResponses());
            globalProgress.ToolCallRequests.Clear();
            
            // TODO: The model can keep calling the same tool call multiple times. We need to handle this case.
                // Recursively call this method to handle tool calls    
                await GenerateAndProcessResponseAsync(globalProgress, modelConfiguration, false, cancellationToken);
        }

        if (isRoot)
        {
            messageStreamWriter.WriteAsync(MessageFactory.SignalPromptComplete(globalProgress.PromptId),
                cancellationToken);
        }

        return;

        void OnChunkReceived(OllamaResponseChunk obj)
        {
            chunkId++;

            if (obj.Message.ToolCalls is not null)
                foreach (var ollamaToolCall in obj.Message.ToolCalls)
                {
                    globalProgress.ToolCallRequests.Add(new OllamaToolCallRequest
                    {
                        Function = ollamaToolCall.Function,
                        Id = ollamaToolCall.Id,
                        ReceivedAt = DateTime.UtcNow,
                    });
                    messageStreamWriter.WriteAsync(
                        MessageFactory.ToolCall(ollamaToolCall.Function, globalProgress.PromptId, ollamaToolCall.Id),
                        cancellationToken);
                }

            if (!string.IsNullOrWhiteSpace(obj.Message.Thinking))
            {
                thinking.Append(obj.Message.Thinking);
                messageStreamWriter.WriteAsync(MessageFactory.ThinkingChunk(obj.Message.Thinking,
                    globalProgress.PromptId), cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(obj.Message.Content))
            {
                if (obj.Message.Role != KnownRoles.Assistant)
                    throw new Exception($"Unexpected role: {obj.Message.Role}");

                content.Append(obj.Message.Content);
                messageStreamWriter.WriteAsync(MessageFactory.ChunkResponse(obj.Message.Content, globalProgress.PromptId,
                    chunkId.ToString()), cancellationToken);
            }

            if (obj.Done)
                chunksComplete = true;
        }


        OllamaChatMessage GetAssistantResponse()
        {
            return new OllamaChatMessage
            {
                Content = content.ToString(),
                ToolCalls = globalProgress.ToolCallRequests.Cast<OllamaToolCall>().ToArray(),
                Thinking = thinking.ToString(),
                Role = KnownRoles.Assistant,
                CreatedAt = DateTime.UtcNow,
            };
        }

        OllamaChatMessage[] GetToolResponses()
        {
            return globalProgress.ToolCallRequests
                .Where(x => x.IsResolved)
                .Select(t => new OllamaChatMessage
                {
                    Content = t.Response,
                    ToolCalls = null,
                    Thinking = null,
                    Role = KnownRoles.Tool,
                    CreatedAt = t.ReceivedAt,
                }).ToArray();
        }
    }
}