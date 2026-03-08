namespace Edgar.Service.Sessions;

public class MessageEnvelope
{
    public MessageHeader[] Headers { get; set; } = [];
    public string? Body { get; set; }

    public string Role => Headers.FirstOrDefault(h => h.Name == KnownHeaders.Role)?.Value ??
                          throw new Exception("Role not found");

    public string? ToolCallId => Headers.FirstOrDefault(h => h.Name == KnownHeaders.ToolCallId)?.Value;
    public string? PromptId => Headers.FirstOrDefault(h => h.Name == KnownHeaders.PromptId)?.Value;
}