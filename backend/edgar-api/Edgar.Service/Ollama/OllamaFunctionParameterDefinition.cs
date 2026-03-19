namespace Edgar.Service.Ollama;

public class OllamaFunctionParameterDefinition
{
    public required string Type { get; set; }
    public required string Description { get; set; }
    public string[]? Enum { get; set; }
}