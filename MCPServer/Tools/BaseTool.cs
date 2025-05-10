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
}

