using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

public class PromptController : ApiController
{
    public PromptController(
        IMediator mediator,
        UserManager<User> userManager)
        : base(mediator, userManager)
    {
    }


    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = CommonModelConstants.Role.Prompter)]
    [Route(nameof(SendUserPromptAsync))]
    public async Task<ActionResult<string>> SendUserPromptAsync([FromBody] UserPromptCommand command) => await Send(command);
}

