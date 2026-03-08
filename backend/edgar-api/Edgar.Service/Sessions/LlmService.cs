using System.Text;
using System.Text.Json;
using Edgar.Service.Ollama;

namespace Edgar.Service.Sessions;

public class LlmService(ILogger<LlmService> logger) : ILlmService
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
        httpRequest.Headers.Add("CF-Access-Client-ID", "<your-client-id-here>");
        httpRequest.Headers.Add("CF-Access-Client-Secret", "<your-client-secret-here>");
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
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                logger.LogInformation("Processing chunk: {Chunk}", line);
                
                var chunk = JsonSerializer.Deserialize<OllamaResponseChunk>(line, jsonOptions);
                
                if (chunk is null)
                    throw new Exception("Chunk is null");

                onChunkReceived?.Invoke(chunk);

                line = await reader.ReadLineAsync(cancellationToken);

                if (line is not null) continue;

                if (!chunk.Done)
                    throw new Exception("Chunk is not done, but no more lines");
            }
            catch (System.Text.Json.JsonException e)
            {
                logger.LogError(e, "Error processing chunk: {Chunk}", line);
                throw;
            }
        }
    }
}