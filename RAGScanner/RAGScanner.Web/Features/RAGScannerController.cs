using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Protocol.Types;

public class RAGScannerController : ApiController
{
    public RAGScannerController(
    IMediator mediator,
    UserManager<User> userManager)
    : base(mediator, userManager)
    {
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = CommonModelConstants.Role.Administrator)]
    [Route(nameof(ScanUrlAsync))]
    public async Task<ActionResult<Result>> ScanUrlAsync(ScanUrlCommand command)
        => await Send(command, CurrentUser);


    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route(nameof(RAGSearchAsync))]
    public async Task<ActionResult<List<RAGSearchResult>>> RAGSearchAsync(RAGSearchCommand command)
        => await Send(command);
}
