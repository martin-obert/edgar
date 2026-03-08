using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Edgar.Service.Ollama;

namespace Edgar.Service.Sessions;

public static class KnownHeaders
{
    public const string PromptId = "prompt-id";
    public const string ToolCallId = "tool-call-id";
    public const string ChunkId = "chunk-id";
    public const string ContentType = "content-type";
    public const string Role = "role";
    public const string Signal = "signal";
}

public static class KnownRoles
{
    public const string Assistant = "assistant";
    public const string User = "user";
    public const string System = "system";
    public const string Tool = "tool";
}

public static class KnownContentTypes
{
    public const string Text = "text/plain";
    public const string Json = "application/json";
    public const string JsonToolCall = "application/json+tool-call";
    public const string Empty = "empty";
}

public static class SignalTypes
{
    public const string RequestComplete = "request_complete";
    public const string ToolCallWaiting = "tool_call_waiting";
}

public class MessageHeaders
{
    public required string Name { get; set; }
    public required string Value { get; set; }
}

public class MessageEnvelope
{
    public MessageHeaders[] Headers { get; set; } = [];
    public string? Body { get; set; }

    public string Role => Headers.FirstOrDefault(h => h.Name == KnownHeaders.Role)?.Value ??
                          throw new Exception("Role not found");

    public string? ToolCallId => Headers.FirstOrDefault(h => h.Name == KnownHeaders.ToolCallId)?.Value;
    public string? PromptId => Headers.FirstOrDefault(h => h.Name == KnownHeaders.PromptId)?.Value;
}

public interface IMessageStreamWriter
{
    Task WriteAsync(MessageEnvelope message);
}

public class WebSocketMessageStreamWriter(WebSocket webSocket) : IMessageStreamWriter
{
    public Task WriteAsync(MessageEnvelope message)
    {
        throw new NotImplementedException();
    }
}

public class MessageHandler(
    IMessageStreamWriter messageStreamWriter,
    ILlmService llmService,
    OllamaModelDefinition modelConfiguration,
    List<OllamaChatMessage> messages,
    ILogger<MessageHandler> logger)
{
    private class PromptProgress
    {
        public List<OllamaToolCallRequest> ToolCalls = new();
        public StringBuilder Content = new();
        public StringBuilder Thinking = new();
        public bool ChunksComplete = false;

        public bool IsComplete => ChunksComplete && ToolCalls.All(t => t.IsResolved) &&
                                  (SubPrompt is null || SubPrompt.IsComplete);

        public required string PromptId { get; init; }
        public string? ParentPromptId { get; set; }

        public bool IsSubPrompt => ParentPromptId is not null;
        public PromptProgress? SubPrompt { get; set; }

        public OllamaChatMessage ToChatMessage()
        {
            return new OllamaChatMessage
            {
                Content = Content.ToString(),
                ToolCalls = ToolCalls.Cast<OllamaToolCall>().ToArray(),
                Thinking = Thinking.ToString(),
                Role = KnownRoles.Assistant,
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
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleToolRoleMessage(MessageEnvelope receivedMessage,
        CancellationToken cancellationToken = default)
    {
        if (_promptProgress is null)
        {
            logger.LogWarning("Tool call received, but no prompt is active");
            return;
        }

        if (receivedMessage.PromptId != _promptProgress.PromptId)
        {
            logger.LogWarning("Tool call received, but prompt id does not match");
            return;
        }

        if (string.IsNullOrWhiteSpace(receivedMessage.ToolCallId))
        {
            logger.LogWarning("Tool call received, but tool call id is null or empty");
            return;
        }

        var pendingToolRequest = _promptProgress.ToolCalls.FirstOrDefault(t => t.Id == receivedMessage.ToolCallId);

        if (pendingToolRequest is null)
        {
            logger.LogWarning("Tool call {ToolCallId} received, but no pending tool call found",
                receivedMessage.ToolCallId);
            return;
        }

        if (pendingToolRequest.IsResolved)
        {
            logger.LogWarning("Tool call {ToolCallId} received, but it is already resolved",
                receivedMessage.ToolCallId);
        }

        pendingToolRequest.IsResolved = true;

        messages.Add(new OllamaChatMessage
        {
            Role = KnownRoles.Tool,
            Content = receivedMessage.Body,
            ToolName = pendingToolRequest.Function.Name,
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
            logger.LogWarning("Prompt progress is null, ignoring message");
            return;
        }

        await llmService.GenerateResponseAsync(messages, OnChunkReceived, modelConfiguration,
            CancellationToken.None);

        var delayBudget = TimeSpan.FromSeconds(10);

        while (!localProgress.IsComplete && delayBudget > TimeSpan.Zero)
        {
            delayBudget -= TimeSpan.FromMilliseconds(100);
            await Task.Delay(100, cancellationToken);
        }

        if (localProgress.ToolCalls.Any())
        {
            await llmService.GenerateResponseAsync(messages, OnChunkReceived, modelConfiguration,
                CancellationToken.None);
        }

        if (localProgress.IsComplete) messages.Add(localProgress.ToChatMessage());

        _promptProgress = null;

        return;

        void OnChunkReceived(OllamaResponseChunk obj)
        {
            if (obj.Done)
                localProgress.ChunksComplete = true;

            if (obj.ToolCalls is not null)
                foreach (var ollamaToolCall in obj.ToolCalls)
                {
                    localProgress.ToolCalls.Add(new OllamaToolCallRequest
                    {
                        Function = ollamaToolCall.Function,
                        Id = ollamaToolCall.Id,
                        Type = ollamaToolCall.Type,
                    });
                    messageStreamWriter.WriteAsync();
                }

            if (!string.IsNullOrWhiteSpace(obj.Message.Thinking))
            {
                localProgress.Thinking.Append(obj.Message.Thinking);
                messageStreamWriter.WriteAsync();
            }

            if (!string.IsNullOrWhiteSpace(obj.Message.Content))
            {
                if (obj.Message.Role != KnownRoles.Assistant)
                    throw new Exception($"Unexpected role: {obj.Message.Role}");

                localProgress.Content.Append(obj.Message.Content);
                messageStreamWriter.WriteAsync();
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
            }

            logger.LogWarning("Tool call received, but no prompt is active");
            return null;
        }

        if (receivedMessage.Role == KnownRoles.Tool)
        {
            var subPrompt = new PromptProgress
            {
                PromptId = Guid.NewGuid().ToString(),
                ParentPromptId = _promptProgress.PromptId,
            };
            _promptProgress.SubPrompt = subPrompt;
            return subPrompt;
        }

        return _promptProgress;
    }
}

public interface ILlmService
{
    Task GenerateResponseAsync(IEnumerable<OllamaChatMessage> chatMessages,
        Action<OllamaResponseChunk> onChunkReceived,
        OllamaModelDefinition modelConfiguration,
        CancellationToken cancellationToken);
}

public class LlmService : ILlmService
{
    private readonly string _baseUrl = "https://ollama.obert.cz";

    public async Task GenerateResponseAsync(IEnumerable<OllamaChatMessage> chatMessages,
        Action<OllamaResponseChunk> onChunkReceived,
        OllamaModelDefinition modelConfiguration,
        CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(_baseUrl);
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(new OllamaChatRequest
        {
            Messages = chatMessages.Prepend(new OllamaChatMessage
            {
                Role = KnownRoles.System,
                Content = modelConfiguration.SystemPrompt
            }),
            Model = modelConfiguration.Model,
            Options = modelConfiguration.Options,
            Stream = true,
            Tools = modelConfiguration.AllTools
        }, jsonOptions);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat");
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        // Critical: ResponseHeadersRead starts streaming immediately
        // instead of buffering the entire response
        using var response = await httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var line = await reader.ReadLineAsync(cancellationToken);
        while (!string.IsNullOrWhiteSpace(line))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chunk = JsonSerializer.Deserialize<OllamaResponseChunk>(line, jsonOptions);
            if (chunk is null)
                throw new Exception("Chunk is null");

            onChunkReceived?.Invoke(chunk);

            line = await reader.ReadLineAsync(cancellationToken);

            if (line is not null) continue;

            if (!chunk.Done)
                throw new Exception("Chunk is not done, but no more lines");
        }
    }
}