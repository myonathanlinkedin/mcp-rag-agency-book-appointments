using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

public class AssignUserToAgencyCommand : IRequest<Result>
{
    public string UserEmail { get; }
    public List<string> Roles { get; }

    public AssignUserToAgencyCommand(string userEmail, List<string> roles)
    {
        UserEmail = userEmail;
        Roles = roles;
    }

    public class AssignUserToAgencyCommandHandler : IRequestHandler<AssignUserToAgencyCommand, Result>
    {
        private readonly IAgencyService agencyService;
        private readonly ILogger<AssignUserToAgencyCommandHandler> logger;
        private readonly IHttpContextAccessor httpContextAccessor;

        public AssignUserToAgencyCommandHandler(
            IAgencyService agencyService,
            ILogger<AssignUserToAgencyCommandHandler> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            this.agencyService = agencyService;
            this.logger = logger;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result> Handle(AssignUserToAgencyCommand request, CancellationToken cancellationToken)
        {
            // Retrieve AgencyEmail from HttpContextAccessor
            var agencyEmail = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(agencyEmail))
            {
                logger.LogWarning("Failed to assign user. No agency email found in HttpContext.");
                return Result.Failure(new[] { "No agency email found in HttpContext." });
            }

            var agency = await agencyService.GetByEmailAsync(agencyEmail);
            if (agency == null)
            {
                logger.LogWarning("Failed to assign user. No agency found for email {AgencyEmail}.", agencyEmail);
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

            logger.LogInformation("Successfully assigned user {UserEmail} to agency {AgencyEmail}.", request.UserEmail, agencyEmail);
            return Result.Success;
        }
    }
}

