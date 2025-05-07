using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

public class IdentityController : ApiController
{
    public IdentityController(
        IMediator mediator,
        UserManager<User> userManager)
        : base(mediator, userManager)
    {
    }

    [HttpPost]
    [Route(nameof(RegisterAsync))]
    public async Task<ActionResult> RegisterAsync(
        RegisterUserCommand command)
        => await Send(command);

    [HttpPost]
    [Route(nameof(LoginAsync))]
    public async Task<ActionResult<UserResponseModel>> LoginAsync(
        LoginUserCommand command)
        => await Send(command);

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route(nameof(RefreshTokenAsync))]
    public async Task<ActionResult<UserResponseModel>> RefreshTokenAsync(
        RefreshTokenCommand command)
        => await Send(command);

    [HttpPut]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route(nameof(ChangePasswordAsync))]
    public async Task<ActionResult> ChangePasswordAsync(
        ChangePasswordCommand command)
        => await Send(command);

    [HttpPut]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = CommonModelConstants.Role.Administrator)]
    [Route(nameof(AssignRoleAsync))]
    public async Task<ActionResult> AssignRoleAsync(
      AssignRoleCommand command)
        => await Send(command);

    [HttpPost]
    [Route(nameof(ResetPasswordAsync))]
    public async Task<ActionResult> ResetPasswordAsync(
        ResetPasswordCommand command)
        => await Send(command);
}
