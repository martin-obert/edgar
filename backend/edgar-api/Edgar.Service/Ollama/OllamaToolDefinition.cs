namespace Edgar.Service.Ollama;

public class OllamaToolDefinition
{
    public string Type => "function";
    public required OllamaFunctionDefinition Function { get; set; }
}