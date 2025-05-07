using MediatR;
using Microsoft.AspNetCore.Identity;

public class UserRequestWrapper<T> : IRequest<Result> where T : BaseCommand<T>
{
    public T Request { get; }
    public IdentityUser User { get; }

    public UserRequestWrapper(T request, IdentityUser user)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        User = user ?? throw new UnauthorizedAccessException("User authentication failed.");
    }
}