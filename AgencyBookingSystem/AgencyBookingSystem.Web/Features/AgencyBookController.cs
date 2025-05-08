using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

// Admin has access to all endpoints here
[Authorize(Policy = CommonModelConstants.Policy.AdminAccess)]
public class AgencyBookController : ApiController
{
    public AgencyBookController(
        IMediator mediator,
        UserManager<User> userManager)
        : base(mediator, userManager)
    {
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = CommonModelConstants.Role.Administrator)]
    [Route(nameof(CreateAgencyAsync))]
    public async Task<ActionResult<Result>> CreateAgencyAsync(CreateAgencyCommand command)
    => await Send(command);

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = CommonModelConstants.Role.Agency)]
    [Route(nameof(AssignUserToAgencyAsync))]
    public async Task<ActionResult<Result>> AssignUserToAgencyAsync(AssignUserToAgencyCommand command)
        => await Send(command);

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = CommonModelConstants.Role.Agency)]
    [Route(nameof(CancelAppointmentAsync))]
    public async Task<ActionResult<Result>> CancelAppointmentAsync(CancelAppointmentCommand command)
        => await Send(command);

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = CommonModelConstants.Role.Agency)]
    [Route(nameof(CreateAppointmentAsync))]
    public async Task<ActionResult<Result>> CreateAppointmentAsync(CreateAppointmentCommand command)
        => await Send(command);

    [HttpPut]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = CommonModelConstants.Role.Agency)]
    [Route(nameof(UpdateAgencySettingsAsync))]
    public async Task<ActionResult<Result>> UpdateAgencySettingsAsync(UpdateAgencySettingsCommand command)
      => await Send(command);

    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = CommonModelConstants.Role.Agency)]
    [Route(nameof(GetAppointmentsByDateAsync))]
    public async Task<ActionResult<List<AppointmentDto>>> GetAppointmentsByDateAsync(GetAppointmentsByDateCommand command)
        => await Send(command);

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = CommonModelConstants.Role.Agency)]
    [Route(nameof(HandleNoShowAsync))]
    public async Task<ActionResult<Result>> HandleNoShowAsync(HandleNoShowCommand command)
        => await Send(command);

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = CommonModelConstants.Role.Agency)]
    [Route(nameof(RescheduleAppointmentAsync))]
    public async Task<ActionResult<Result>> RescheduleAppointmentAsync(RescheduleAppointmentCommand command)
        => await Send(command);
}
