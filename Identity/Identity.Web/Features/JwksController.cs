using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

public class JwksController : ApiController
{
    public JwksController(
        IMediator mediator,
        UserManager<User> userManager)
        : base(mediator, userManager)
    {
    }

    [HttpGet]
    [Route(nameof(GetPublicKeyAsync))]
    public async Task<ActionResult<JsonWebKey>> GetPublicKeyAsync([FromQuery] GetPublicKeyCommand command) 
        => await Send(command);
}
