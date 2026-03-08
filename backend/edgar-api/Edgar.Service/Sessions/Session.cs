using Edgar.Service.Ollama;

namespace Edgar.Service.Sessions;

public class Session
{
    public Guid Id { get; set; }
    public OllamaModelDefinition? ModelConfiguration { get; set; }
}