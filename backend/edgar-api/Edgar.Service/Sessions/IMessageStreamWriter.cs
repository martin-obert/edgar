namespace Edgar.Service.Sessions;

public interface IMessageStreamWriter
{
    Task WriteAsync(MessageEnvelope message, CancellationToken cancellationToken = default);
}