namespace Edgar.Service.Ollama;

public class OllamaToolCallRequest : OllamaToolCall
{
    public bool IsResolved { get; set; }

    public string? Response { get; private set; }
    
    public required DateTime ReceivedAt { get; init; }

    public void Resolve(string? receivedMessageBody)
    {
        IsResolved = true;
        Response = receivedMessageBody;
    }
}