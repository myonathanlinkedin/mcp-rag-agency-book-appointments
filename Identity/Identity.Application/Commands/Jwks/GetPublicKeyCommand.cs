using MediatR;
using Microsoft.IdentityModel.Tokens;

public class GetPublicKeyCommand : IRequest<Result<JsonWebKey>>
{
    public class GetPublicKeyCommandHandler : IRequestHandler<GetPublicKeyCommand, Result<JsonWebKey>>
    {
        private readonly IIdentity identity;

        public GetPublicKeyCommandHandler(IIdentity identity)
            => this.identity = identity;

        public Task<Result<JsonWebKey>> Handle(
            GetPublicKeyCommand request,
            CancellationToken cancellationToken)
            => Task.FromResult(this.identity.GetPublicKey());
    }
}