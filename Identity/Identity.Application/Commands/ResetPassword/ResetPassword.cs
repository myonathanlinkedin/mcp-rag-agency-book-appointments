using MediatR;

public class ResetPasswordCommand : IRequest<Result>
{
    public string Email { get; set; } = default!;

    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
    {
        private readonly IIdentity identity;

        public ResetPasswordCommandHandler(IIdentity identity)
        {
            this.identity = identity;
        }

        public async Task<Result> Handle(
            ResetPasswordCommand request,
            CancellationToken cancellationToken)
            => await identity.ResetPassword(request.Email, cancellationToken);
    }
}
