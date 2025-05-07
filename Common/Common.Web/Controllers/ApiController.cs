using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

[ApiController]
[Route("api/[controller]/[action]")]
public abstract class ApiController : ControllerBase
{
    protected const string Id = "{id}";
    protected const string PathSeparator = "/";

    private readonly IMediator mediator;
    private readonly UserManager<User> userManager;

    protected ApiController(IMediator mediator, UserManager<User> userManager)
    {
        this.mediator = mediator;
        this.userManager = userManager;
    }

    protected User CurrentUser
        => GetAuthenticatedUser() ?? throw new UnauthorizedAccessException("User authentication failed: No authenticated user found.");

    protected Task<ActionResult> Send<TCommand>(TCommand request, User user)
        where TCommand : BaseCommand<TCommand>
        => mediator.Send(new UserRequestWrapper<TCommand>(request, user)).ToActionResult();

    protected Task<ActionResult<TResult>> Send<TResult>(IRequest<TResult> request)
        => mediator.Send(request).ToActionResult();

    protected Task<ActionResult<TResult>> Send<TResult>(IRequest<Result<TResult>> request)
        => mediator.Send(request).ToActionResult();

    protected Task<ActionResult> Send(IRequest<Result> request)
        => mediator.Send(request).ToActionResult();

    protected Task<ActionResult> Send(IRequest<Stream> request)
    {
        var headers = Response.GetTypedHeaders();
        headers.CacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromDays(30)
        };
        headers.Expires = new DateTimeOffset(DateTime.UtcNow.AddDays(30));

        return mediator.Send(request).ToActionResult();
    }

    private User? GetAuthenticatedUser()
    {
        var userPrincipal = HttpContext?.User;
        return userPrincipal != null ? userManager.GetUserAsync(userPrincipal).GetAwaiter().GetResult() : null;
    }
}