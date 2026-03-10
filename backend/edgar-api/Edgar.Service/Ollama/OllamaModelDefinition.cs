using System.Text.Json;

namespace Edgar.Service.Ollama;

public class OllamaFunctionParameterDefinition
{
    public required string Type { get; set; }
    public required string Description { get; set; }
    public string[]? Enum { get; set; }
}

public class OllamaFunctionParametersDefinition
{
    public required string Type { get; set; }
    public string[]? Required { get; set; }
    public required Dictionary<string, OllamaFunctionParameterDefinition> Properties { get; set; }
}

public class OllamaFunctionDefinition
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public OllamaFunctionParametersDefinition? Parameters { get; set; }
}

public class OllamaToolDefinition
{
    public string Type => "function";
    public required OllamaFunctionDefinition Function { get; set; }
}

public class OllamaModelOptions
{
    public int Seed { get; set; }
    public float Temperature { get; set; }
    public float? TopP { get; set; }
    public int? TopK { get; set; }
    public int? NumPredict { get; set; }
    public int? NumCtx { get; set; }
    public string? Stop { get; set; }
    public float? MinP { get; set; }
}

public class OllamaModelDefinition
{
    public required string Model { get; set; }
    public required string SystemPrompt { get; set; }
    public OllamaToolDefinition[]? AllTools { get; set; }
    public OllamaModelOptions? Options { get; set; }
}

public class OllamaToolCallFunction
{
    public required string Name { get; set; }
    public int? Index { get; set; }
    public Dictionary<string, JsonElement>? Arguments { get; set; }
}

public class OllamaToolCallRequest : OllamaToolCall
{
    public bool IsResolved { get; set; }
}

public class OllamaToolCall
{
    public required string Id { get; set; }
    // public required string Type { get; set; }
    public required OllamaToolCallFunction Function { get; set; }
}

public class OllamaChatMessage
{
    public required string Role { get; set; }
    public string? Content { get; set; }
    public string? Thinking { get; set; }
    public OllamaToolCall[]? ToolCalls { get; set; }
    public string? ToolName { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class OllamaResponseChunk
{
    public required string Model { get; set; }
    public required string CreatedAt { get; set; }
    public required OllamaChatMessage Message { get; set; }

    public required bool Done { get; set; }
    public string? DoneReason { get; set; }
    public long? TotalDuration { get; set; }
}

public class OllamaChatRequest
{
    public required string Model { get; set; }
    public required IEnumerable<OllamaChatMessage> Messages { get; set; }
    public OllamaToolDefinition[]? Tools { get; set; }
    public bool? Stream { get; set; }
    public OllamaModelOptions? Options { get; set; }
}