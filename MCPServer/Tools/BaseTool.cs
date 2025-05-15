using ModelContextProtocol.Server;
using Serilog;

[McpServerToolType]
public class BaseTool
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public BaseTool( IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    protected string LogAndReturnMissingToken()
    {
        Log.Warning("Authentication token is missing.");
        return "Authentication token is missing or invalid.";
    }

    protected string? GetTokenFromHttpContext()
    {
        var token = httpContextAccessor.HttpContext?.Request?.Headers["Authorization"].ToString();
        if (!string.IsNullOrWhiteSpace(token) && token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return token.Substring("Bearer ".Length).Trim();
        }

        return null;
    }

    protected string GetBearerToken()
    {
        return $"Bearer {GetTokenFromHttpContext()}";
    }

    protected async Task<string> GetErrorMessage(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                return $"Request failed with status code: {response.StatusCode}";
            }

            return $"Request failed with status code: {response.StatusCode}, details: {content}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error parsing error response");
            return $"Request failed with status code: {response.StatusCode}";
        }
    }
}

