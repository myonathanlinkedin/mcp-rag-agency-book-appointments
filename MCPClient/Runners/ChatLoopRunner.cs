using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

public static class ChatLoopRunner
{
    public static async Task RunAsync(IChatClient chatClient, IEnumerable<McpClientTool> tools)
    {
        var messages = new List<ChatMessage>();

        while (true)
        {
            Console.Write("Q: ");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) continue;

            messages.Add(new(ChatRole.User, input));

            var results = chatClient.GetStreamingResponseAsync(messages, new() { Tools = [.. tools] });
            await foreach (var update in results)
            {
                Console.Write(update);
            }

            Console.WriteLine("\n");
        }
    }
}
