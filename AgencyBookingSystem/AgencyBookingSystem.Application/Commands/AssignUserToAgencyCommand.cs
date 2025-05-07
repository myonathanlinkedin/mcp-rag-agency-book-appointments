using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

public class AssignUserToAgencyCommand : IRequest<Result>
{
    public string AgencyEmail { get; }
    public string UserEmail { get; }
    public List<string> Roles { get; }

    public AssignUserToAgencyCommand(string agencyEmail, string userEmail, List<string> roles)
    {
        AgencyEmail = agencyEmail;
        UserEmail = userEmail;
        Roles = roles;
    }
}

public class AssignUserToAgencyCommandHandler : IRequestHandler<AssignUserToAgencyCommand, Result>
{
    private readonly IAgencyService agencyService;
    private readonly ILogger<AssignUserToAgencyCommandHandler> logger;

    public AssignUserToAgencyCommandHandler(IAgencyService agencyService, ILogger<AssignUserToAgencyCommandHandler> logger)
    {
        this.agencyService = agencyService;
        this.logger = logger;
    }

    public async Task<Result> Handle(AssignUserToAgencyCommand request, CancellationToken cancellationToken)
    {
        var agency = await agencyService.GetByEmailAsync(request.AgencyEmail);
        if (agency == null)
        {
            logger.LogWarning("Failed to assign user. No agency found for email {AgencyEmail}.", request.AgencyEmail);
            return Result.Failure(new[] { "No agency found for this email." });
        }

        var user = await agencyService.GetAgencyUserByEmailAsync(request.UserEmail);
        if (user == null)
        {
            logger.LogWarning("Failed to assign user. No user found for email {UserEmail}.", request.UserEmail);
            return Result.Failure(new[] { "No user found for this email." });
        }

        var result = await agencyService.AssignUserToAgencyAsync(agency.Id, user.Email, user.FullName, request.Roles, cancellationToken);

        if (!result.Succeeded)
        {
            logger.LogWarning("Failed to assign user to agency {AgencyId}. Reason: {Errors}", agency.Id, string.Join(", ", result.Errors));
            return Result.Failure(result.Errors);
        }

        logger.LogInformation("Successfully assigned user {UserEmail} to agency {AgencyEmail}.", request.UserEmail, request.AgencyEmail);
        return Result.Success;
    }
}
