using ModelContextProtocol.Server;

[McpServerToolType]
public class BaseTool
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public BaseTool( IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
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
}

