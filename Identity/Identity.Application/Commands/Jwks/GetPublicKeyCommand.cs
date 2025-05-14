using MediatR;
using Microsoft.IdentityModel.Tokens;

public class GetPublicKeyCommand : IRequest<Result<JsonWebKey>>
{
    public class GetPublicKeyCommandHandler : IRequestHandler<GetPublicKeyCommand, Result<JsonWebKey>>
    {
        private readonly IJwksProviderService jwksProvider;

        public GetPublicKeyCommandHandler(IJwksProviderService jwksProvider)
            => this.jwksProvider = jwksProvider;

        public async Task<Result<JsonWebKey>> Handle(
            GetPublicKeyCommand request,
            CancellationToken cancellationToken)
        {
            var jwk = await jwksProvider.GetPublicKeyAsync();
            return Result<JsonWebKey>.SuccessWith(jwk);
        }
    }
}