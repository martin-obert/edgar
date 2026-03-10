using Edgar.Service.Ollama;

namespace Edgar.Service.Sessions;

public class ChatMessageBag(IChatLog log)
{
    private readonly List<OllamaChatMessage> _messages = [];

    public void Add(OllamaChatMessage ollamaChatMessage)
    {
        log.LogMessage(ollamaChatMessage);
        _messages.Add(ollamaChatMessage);
    }

    public IEnumerable<OllamaChatMessage> Prepend(OllamaChatMessage ollamaChatMessage)
    {
        return _messages.Prepend(ollamaChatMessage);
    }
}