namespace Edgar.Service.Ollama;

public class OllamaFunctionParametersDefinition
{
    public required string Type { get; set; }
    public string[]? Required { get; set; }
    public required Dictionary<string, OllamaFunctionParameterDefinition> Properties { get; set; }
}