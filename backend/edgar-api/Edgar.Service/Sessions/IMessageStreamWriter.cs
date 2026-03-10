namespace Edgar.Service.Sessions;

public interface IMessageStreamWriter
{
    void WriteAsync(MessageEnvelope message, CancellationToken cancellationToken = default);
}