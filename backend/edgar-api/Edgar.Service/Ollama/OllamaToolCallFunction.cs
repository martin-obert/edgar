using System.Text.Json;

namespace Edgar.Service.Ollama;

public class OllamaToolCallFunction
{
    public required string Name { get; set; }
    public int? Index { get; set; }
    public Dictionary<string, JsonElement>? Arguments { get; set; }
}