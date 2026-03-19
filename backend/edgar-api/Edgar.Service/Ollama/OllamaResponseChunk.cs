namespace Edgar.Service.Ollama;

public class OllamaResponseChunk
{
    public required string Model { get; set; }
    public required string CreatedAt { get; set; }
    public required OllamaChatMessage Message { get; set; }

    public required bool Done { get; set; }
    public string? DoneReason { get; set; }
    public long? TotalDuration { get; set; }
}