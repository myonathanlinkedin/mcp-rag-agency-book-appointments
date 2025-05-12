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
        private readonly IAgencyUserService agencyUserService;
        private readonly ILogger<AssignUserToAgencyCommandHandler> logger;
        private readonly IHttpContextAccessor httpContextAccessor;

        public AssignUserToAgencyCommandHandler(
            IAgencyService agencyService,
            IAgencyUserService agencyUserService,
            ILogger<AssignUserToAgencyCommandHandler> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            this.agencyService = agencyService;
            this.agencyUserService = agencyUserService;
            this.logger = logger;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result> Handle(AssignUserToAgencyCommand request, CancellationToken cancellationToken)
        {
            // Get user context
            var user = httpContextAccessor.HttpContext?.User;
            var userEmail = user?.FindFirst(ClaimTypes.Name)?.Value;
            var isAdmin = user?.IsInRole(CommonModelConstants.Role.Administrator) ?? false;
            var isAgency = user?.IsInRole(CommonModelConstants.Role.Agency) ?? false;

            if (string.IsNullOrEmpty(userEmail))
            {
                logger.LogWarning("User assignment failed. No valid user email found in HttpContext.");
                return Result.Failure(new[] { "No valid user email found in HttpContext." });
            }

            // Determine which agency email to use
            string agencyEmail = request.AgencyEmail ?? userEmail;

            // Validate authorization
            if (request.AgencyEmail != null && !isAdmin)
            {
                logger.LogWarning("Unauthorized assignment attempt. Only administrators can assign users to another agency.");
                return Result.Failure(new[] { "Only administrators can assign users to another agency." });
            }

            if (request.AgencyEmail == null && !isAgency)
            {
                logger.LogWarning("Unauthorized assignment attempt. User must have Agency role.");
                return Result.Failure(new[] { "User must have Agency role to assign users." });
            }

            // Get agency
            var agency = await agencyService.GetByEmailAsync(agencyEmail);
            if (agency == null)
            {
                logger.LogWarning("Failed to assign user. No agency found for email {AgencyEmail}.", agencyEmail);
                return Result.Failure(new[] { "Agency not found." });
            }

            // Validate agency is approved
            if (!agency.IsApproved)
            {
                logger.LogWarning("Failed to assign user. Agency {AgencyEmail} is not approved.", agencyEmail);
                return Result.Failure(new[] { "Cannot assign users to an unapproved agency." });
            }

            // Check if user already exists
            var existingUser = await agencyUserService.GetByEmailAsync(request.UserEmail);
            if (existingUser != null)
            {
                logger.LogWarning("Failed to assign user. User {UserEmail} already exists.", request.UserEmail);
                return Result.Failure(new[] { "A user with this email already exists." });
            }

            // Validate roles
            if (!request.Roles.Any())
            {
                logger.LogWarning("Failed to assign user. No roles provided.");
                return Result.Failure(new[] { "At least one role must be provided." });
            }

            foreach (var role in request.Roles)
            {
                if (!CommonModelConstants.AgencyRole.ValidRoles.Contains(role))
                {
                    logger.LogWarning("Failed to assign user. Invalid role: {Role}", role);
                    return Result.Failure(new[] { $"Invalid role: {role}" });
                }
            }

            // Create and assign the user
            var result = await agencyService.AssignUserToAgencyAsync(
                agency.Id,
                request.UserEmail,
                request.UserEmail.Split('@')[0], // Use email prefix as temporary name
                request.Roles,
                cancellationToken);

            if (!result.Succeeded)
            {
                logger.LogWarning("Failed to assign user to agency. Errors: {Errors}", string.Join(", ", result.Errors));
                return result;
            }

            logger.LogInformation("Successfully assigned user {UserEmail} to agency {AgencyEmail}.", request.UserEmail, agencyEmail);
            return Result.Success;
        }
    }
}
