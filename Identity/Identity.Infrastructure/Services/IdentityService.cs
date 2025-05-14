using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

public class IdentityService : IIdentity
{
    private const string InvalidErrorMessage = "Invalid credentials.";
    private readonly UserManager<User> userManager;
    private readonly IJwtGeneratorService jwtGenerator;
    private readonly IEventDispatcher eventDispatcher;
    private readonly ILogger<IdentityService> logger;

    public IdentityService(
        UserManager<User> userManager,
        IJwtGeneratorService jwtGenerator,
        IEventDispatcher eventDispatcher,
        ILogger<IdentityService> logger)
    {
        this.userManager = userManager;
        this.jwtGenerator = jwtGenerator;
        this.eventDispatcher = eventDispatcher;
        this.logger = logger;
    }

    public async Task<Result<IUser>> Register(UserRequestModel userRequest, CancellationToken cancellationToken)
    {
        var user = new User(userRequest.Email);
        var identityResult = await userManager.CreateAsync(user, userRequest.Password);
        var errors = identityResult.Errors.Select(e => e.Description);

        if (identityResult.Succeeded)
        {
            logger.LogInformation("User registered successfully with email: {Email}", userRequest.Email);
            await eventDispatcher.Dispatch(new UserRegisteredEvent(userRequest.Email, userRequest.Password), cancellationToken);
            return Result<IUser>.SuccessWith(user);
        }

        logger.LogWarning("User registration failed for email: {Email}. Errors: {Errors}", userRequest.Email, string.Join(", ", errors));
        return Result<IUser>.Failure(errors);
    }

    public async Task<Result<UserResponseModel>> Login(UserRequestModel userRequest, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(userRequest.Email);
        if (user == null)
        {
            logger.LogWarning("Login failed. User not found for email: {Email}", userRequest.Email);
            return Result<UserResponseModel>.Failure(new[] { InvalidErrorMessage });
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, userRequest.Password);
        if (!passwordValid)
        {
            logger.LogWarning("Login failed. Invalid password for email: {Email}", userRequest.Email);
            return Result<UserResponseModel>.Failure(new[] { InvalidErrorMessage });
        }

        var tokens = await jwtGenerator.GenerateToken(user);
        logger.LogInformation("User logged in successfully with email: {Email}", userRequest.Email);
        return Result<UserResponseModel>.SuccessWith(new UserResponseModel(tokens.AccessToken, tokens.RefreshToken));
    }

    public async Task<Result<string>> RefreshToken(RefreshTokenRequestModel refreshTokenRequest, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(refreshTokenRequest.UserId);
        if (user == null)
        {
            logger.LogWarning("Refresh token failed. User not found for UserId: {UserId}", refreshTokenRequest.UserId);
            return Result<string>.Failure(new[] { InvalidErrorMessage });
        }

        try
        {
            var newAccessToken = await jwtGenerator.RefreshToken(refreshTokenRequest.UserId, refreshTokenRequest.RefreshToken);
            logger.LogInformation("Access token refreshed successfully for UserId: {UserId}", refreshTokenRequest.UserId);
            return Result<string>.SuccessWith(newAccessToken);
        }
        catch (SecurityTokenException ex)
        {
            logger.LogWarning("Refresh token failed for UserId: {UserId}. Error: {Error}", refreshTokenRequest.UserId, ex.Message);
            return Result<string>.Failure(new[] { "Invalid or expired refresh token." });
        }
    }

    public async Task<Result> ChangePassword(ChangePasswordRequestModel changePasswordRequest, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(changePasswordRequest.UserId);
        if (user == null)
        {
            logger.LogWarning("Change password failed. User not found for UserId: {UserId}", changePasswordRequest.UserId);
            return Result.Failure(new[] { InvalidErrorMessage });
        }

        var identityResult = await userManager.ChangePasswordAsync(user, changePasswordRequest.CurrentPassword, changePasswordRequest.NewPassword);
        var errors = identityResult.Errors.Select(e => e.Description);

        if (identityResult.Succeeded)
        {
            logger.LogInformation("Password changed successfully for email: {Email}", user.Email);
            await eventDispatcher.Dispatch(new PasswordChangedEvent(user.Email, changePasswordRequest.NewPassword), cancellationToken);
            return Result.Success;
        }

        logger.LogWarning("Password change failed for email: {Email}. Errors: {Errors}", user.Email, string.Join(", ", errors));
        return Result.Failure(errors);
    }

    public async Task<Result> ResetPassword(string email, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            logger.LogWarning("Password reset failed. User not found for email: {Email}", email);
            return Result.Failure(new[] { InvalidErrorMessage });
        }

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var newPassword = PasswordGenerator.Generate(CommonModelConstants.Identity.DefaultPasswordLength);
        var identityResult = await userManager.ResetPasswordAsync(user, resetToken, newPassword);
        var errors = identityResult.Errors.Select(e => e.Description);

        if (identityResult.Succeeded)
        {
            logger.LogInformation("Password reset successfully for email: {Email}", email);
            await eventDispatcher.Dispatch(new PasswordResetEvent(user.Email, newPassword), cancellationToken);
            return Result.Success;
        }

        logger.LogWarning("Password reset failed for email: {Email}. Errors: {Errors}", email, string.Join(", ", errors));
        return Result.Failure(errors);
    }

    public async Task<Result> AssignRoleAsync(string email, string roleName, CancellationToken cancellationToken)
    {
        try
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                logger.LogWarning("Role assignment failed. User not found for email: {Email}", email);
                return Result.Failure(new[] { "User not found." });
            }

            var roleExists = await userManager.IsInRoleAsync(user, roleName);
            if (roleExists)
            {
                logger.LogInformation("User {Email} is already assigned to role {RoleName}.", email, roleName);
                return Result.Failure(new[] { "User is already in this role." });
            }

            var roleResult = await userManager.AddToRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
            {
                logger.LogError("Failed to assign role {RoleName} to user {Email}. Errors: {Errors}",
                    roleName, email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                return Result.Failure(roleResult.Errors.Select(e => e.Description));
            }

            logger.LogInformation("Successfully assigned role {RoleName} to user {Email}.", roleName, email);
            return Result.Success;
        }
        catch (Exception ex)
        {
            logger.LogError("Error assigning role {RoleName} to user {Email}. Exception: {Exception}", roleName, email, ex.Message);
            return Result.Failure(new[] { "An unexpected error occurred." });
        }
    }

    public Result<JsonWebKey> GetPublicKey()
    {
        return Result<JsonWebKey>.SuccessWith(jwtGenerator.GetPublicKey());
    }
}
