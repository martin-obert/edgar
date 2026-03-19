using System.Text.Json.Nodes;
using Edgar.Service.Sessions;

namespace Edgar.Service.Ollama;

public class OllamaChatRequest
{
    public required string Model { get; set; }
    public required IEnumerable<OllamaChatMessage> Messages { get; set; }
    public OllamaToolDefinition[]? Tools { get; set; }
    public required bool Stream { get; set; }
    public JsonValue Think { get; set; } = JsonValue.Create(false);
    public string? KeepAlive { get; set; }
    public OllamaModelOptions? Options { get; set; }
}