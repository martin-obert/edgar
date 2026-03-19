using System.Text.Json.Nodes;

namespace Edgar.Service.Sessions;

public class MessageOptions
{
    public JsonValue Think { get; set; } = JsonValue.Create(false);
    public bool Stream { get; set; } = false;
    public string? KeepAlive { get; set; }

    public static MessageOptions FromEnvelopeHeaders(MessageEnvelope envelope)
    {
        return new MessageOptions
        {
            Think = string.IsNullOrWhiteSpace(envelope.Think) || envelope.Think.Equals("false", StringComparison.OrdinalIgnoreCase)
                ? JsonValue.Create(false)
                : JsonValue.Create(envelope.Think),
            Stream = envelope.Stream,
            KeepAlive = string.IsNullOrWhiteSpace(envelope.KeepAlive) ? null : envelope.KeepAlive,
        };
    }
}


