namespace Edgar.Service.Ollama;

public class OllamaChatMessage
{
    public required string Role { get; set; }
    public string? Content { get; set; }
    public string? Thinking { get; set; }
    public OllamaToolCall[]? ToolCalls { get; set; }
    public string? ToolName { get; set; }
    public DateTime? CreatedAt { get; set; }
}