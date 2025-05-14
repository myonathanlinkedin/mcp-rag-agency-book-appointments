using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using System.Security.Claims;
using System.Text;

public class MCPServerRequester : IMCPServerRequester
{
    private readonly IChatClient chatClient;
    private readonly IEnumerable<McpClientTool> tools;
    private readonly ILogger<MCPServerRequester> logger;
    private readonly IChatMessageStore messageStore;
    private readonly IHttpContextAccessor httpContextAccessor;

    public MCPServerRequester(IChatClient chatClient, IEnumerable<McpClientTool> tools, IChatMessageStore messageStore, ILogger<MCPServerRequester> logger, IHttpContextAccessor httpContextAccessor)
    {
        this.chatClient = chatClient;
        this.tools = tools;
        this.logger = logger;
        this.messageStore = messageStore;
        this.httpContextAccessor = httpContextAccessor;
    }

    private string GetUserEmailFromContext()
    {
        var user = httpContextAccessor.HttpContext?.User;
        var userEmail = user?.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(userEmail))
        {
            logger.LogWarning("User email not found in claims");
            throw new UnauthorizedAccessException("User email not found. Please log in again.");
        }

        return userEmail;
    }

    public async Task<Result<string>> RequestAsync(string prompt, ChatRole? chatRole = null, bool useSession = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var userEmail = useSession ? GetUserEmailFromContext() : string.Empty;

            List<ChatMessage> messages = useSession ? messageStore.GetMessages(userEmail) : new List<ChatMessage>();

            messages.Add(new ChatMessage(chatRole ?? ChatRole.User, prompt));

            List<ChatResponseUpdate> updates = new List<ChatResponseUpdate>();

            var results = chatClient.GetStreamingResponseAsync(messages, new() { Tools = tools.Cast<AITool>().ToList() }, cancellationToken);

            StringBuilder responseBuilder = new StringBuilder();

            await foreach (var update in results)
            {
                responseBuilder.Append(update);
                updates.Add(update);
            }

            messages.AddMessages(updates);

            if(useSession)
                messageStore.SaveMessages(userEmail, messages);

            return Result<string>.SuccessWith(responseBuilder.ToString());
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt");
            return Result<string>.Failure(new List<string> { "Unauthorized: Please log in to continue." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while processing the request.");
            return Result<string>.Failure(new List<string> { $"An error occurred: {ex.Message}" });
        }
    }
}
