using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

public class AssignRoleCommand : IRequest<Result>
{
    public string Email { get; }
    public string RoleName { get; }

    public AssignRoleCommand(string email, string roleName)
    {
        Email = email;
        RoleName = roleName;
    }

    public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, Result>
    {
        private readonly UserManager<User> userManager;
        private readonly ILogger<AssignRoleCommandHandler> logger;

        public AssignRoleCommandHandler(UserManager<User> userManager, ILogger<AssignRoleCommandHandler> logger)
        {
            this.userManager = userManager;
            this.logger = logger;
        }

        public async Task<Result> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    logger.LogWarning("Role assignment failed. User not found for email: {Email}", request.Email);
                    return Result.Failure(new[] { "User not found." });
                }

                var roleExists = await userManager.IsInRoleAsync(user, request.RoleName);
                if (roleExists)
                {
                    logger.LogInformation("User {Email} is already assigned to role {RoleName}.", request.Email, request.RoleName);
                    return Result.Failure(new[] { "User is already in this role." });
                }

                var roleResult = await userManager.AddToRoleAsync(user, request.RoleName);
                if (!roleResult.Succeeded)
                {
                    logger.LogError("Failed to assign role {RoleName} to user {Email}. Errors: {Errors}",
                        request.RoleName, request.Email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    return Result.Failure(roleResult.Errors.Select(e => e.Description));
                }

                logger.LogInformation("Successfully assigned role {RoleName} to user {Email}.", request.RoleName, request.Email);
                return Result.Success;
            }
            catch (Exception ex)
            {
                logger.LogError("Error assigning role {RoleName} to user {Email}. Exception: {Exception}", request.RoleName, request.Email, ex.Message);
                return Result.Failure(new[] { "An unexpected error occurred." });
            }
        }
    }
}
