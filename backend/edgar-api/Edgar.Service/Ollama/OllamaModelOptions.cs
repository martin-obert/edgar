using System.Text.Json.Nodes;

namespace Edgar.Service.Ollama;

public class OllamaModelOptions
{
    public int? Seed { get; set; }
    public float Temperature { get; set; }
    public float? TopP { get; set; }
    public int? TopK { get; set; }
    public int? NumPredict { get; set; }
    public int? NumCtx { get; set; }
    public string? Stop { get; set; }
    public float? MinP { get; set; }
    public JsonValue Think { get; set; } = JsonValue.Create(false);
    public bool Stream { get; set; } = true;
    public string? KeepAlive { get; set; }
}