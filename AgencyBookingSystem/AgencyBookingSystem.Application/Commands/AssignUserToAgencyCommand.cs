using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

public class AssignUserToAgencyCommand : IRequest<Result>
{
    public string? AgencyEmail { get; } // Optional: Admin must provide, otherwise retrieved from context
    public string UserEmail { get; }
    public List<string> Roles { get; }

    public AssignUserToAgencyCommand(string? agencyEmail, string userEmail, List<string> roles)
    {
        AgencyEmail = agencyEmail;
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
            var user = httpContextAccessor.HttpContext?.User;
            var authenticatedEmail = user?.FindFirst(ClaimTypes.Email)?.Value;
            var isAdmin = user?.IsInRole(CommonModelConstants.Role.Administrator) ?? false;

            if (string.IsNullOrEmpty(authenticatedEmail))
            {
                logger.LogWarning("Failed to assign user. No valid authenticated user email found.");
                return Result.Failure(new[] { "No valid authenticated user email found." });
            }

            // Determine agency email: Admin must provide it, otherwise retrieve from user context
            string resolvedAgencyEmail = request.AgencyEmail ?? authenticatedEmail;

            if (request.AgencyEmail != null && !isAdmin)
            {
                logger.LogWarning("Unauthorized assignment attempt. Only administrators can assign users to another agency.");
                return Result.Failure(new[] { "Only administrators can assign users to another agency." });
            }

            var agency = await agencyService.GetByEmailAsync(resolvedAgencyEmail);
            if (agency == null)
            {
                logger.LogWarning("Failed to assign user. No agency found for email {AgencyEmail}.", resolvedAgencyEmail);
                return Result.Failure(new[] { "No agency found for this email." });
            }

            var userExists = await agencyService.GetAgencyUserByEmailAsync(request.UserEmail);
            if (userExists == null)
            {
                logger.LogWarning("Failed to assign user. No user found for email {UserEmail}.", request.UserEmail);
                return Result.Failure(new[] { "No user found for this email." });
            }

            var result = await agencyService.AssignUserToAgencyAsync(agency.Id, userExists.Email, userExists.FullName, request.Roles, cancellationToken);

            if (!result.Succeeded)
            {
                logger.LogWarning("Failed to assign user to agency {AgencyId}. Reason: {Errors}", agency.Id, string.Join(", ", result.Errors));
                return Result.Failure(result.Errors);
            }

            logger.LogInformation("Successfully assigned user {UserEmail} to agency {AgencyEmail}.", request.UserEmail, resolvedAgencyEmail);
            return Result.Success;
        }
    }
}
