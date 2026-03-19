namespace Edgar.Service.Ollama;

public class OllamaModelDefinition
{
    public required string Model { get; set; }
    public required string SystemPrompt { get; set; }
    public OllamaToolDefinition[]? AllTools { get; set; }
    public OllamaModelOptions? Options { get; set; }
}