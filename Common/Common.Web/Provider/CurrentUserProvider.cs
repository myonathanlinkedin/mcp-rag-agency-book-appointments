using System.Security.Claims;
using Microsoft.AspNetCore.Http;

public class CurrentUserProvider : ICurrentUser
{
    public CurrentUserProvider(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;

        if (user == null)
        {
            throw new InvalidOperationException("This request does not have an authenticated user.");
        }

        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public string UserId { get; }
}