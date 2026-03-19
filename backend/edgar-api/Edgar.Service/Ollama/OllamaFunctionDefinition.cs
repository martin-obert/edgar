namespace Edgar.Service.Ollama;

public class OllamaFunctionDefinition
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public OllamaFunctionParametersDefinition? Parameters { get; set; }
}