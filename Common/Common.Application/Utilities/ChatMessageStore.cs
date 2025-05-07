using Microsoft.Extensions.AI;

public class ChatMessageStore : IChatMessageStore
{
    private readonly Dictionary<string, List<ChatMessage>> store = new();

    public List<ChatMessage> GetMessages(string sessionId)
    {
        if (!store.ContainsKey(sessionId))
            store[sessionId] = new List<ChatMessage>();

        return store[sessionId];
    }

    public void SaveMessages(string sessionId, List<ChatMessage> messages)
    {
        store[sessionId] = messages;
    }
}
