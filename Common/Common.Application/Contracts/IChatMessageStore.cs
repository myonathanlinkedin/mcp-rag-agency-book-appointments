using Microsoft.Extensions.AI;

public interface IChatMessageStore
{
    List<ChatMessage> GetMessages(string sessionId);
    void SaveMessages(string sessionId, List<ChatMessage> messages);
}
