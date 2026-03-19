namespace Edgar.Service.Ollama;

public class OllamaToolCall
{
    public required string Id { get; set; }
    // public required string Type { get; set; }
    public required OllamaToolCallFunction Function { get; set; }
}