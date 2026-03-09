
namespace Edgar.Service.Sessions;

public interface IChatRepository
{
    ChatMessageBag GetMessagesForSession(Guid sessionId);
}