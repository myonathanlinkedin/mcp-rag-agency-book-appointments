using MediatR;

public class RefreshTokenCommand : RefreshTokenRequestModel, IRequest<Result<UserResponseModel>>
{
    public RefreshTokenCommand(string userId, string refreshToken) : base(userId, refreshToken)
    {
    }

    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<UserResponseModel>>
    {
        private readonly IIdentity identity;

        public RefreshTokenCommandHandler(IIdentity identity)
            => this.identity = identity;

        public async Task<Result<UserResponseModel>> Handle(
            RefreshTokenCommand request,
            CancellationToken cancellationToken)
        {
            var newAccessToken = await identity.RefreshToken(request, cancellationToken);
            return newAccessToken.Succeeded
                ? Result<UserResponseModel>.SuccessWith(new UserResponseModel(newAccessToken.Data, request.RefreshToken))
                : Result<UserResponseModel>.Failure(newAccessToken.Errors);
        }
    }
}
