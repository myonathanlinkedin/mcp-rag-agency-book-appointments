using ModelContextProtocol.Server;
using Serilog;
using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

public sealed class IdentityTools : BaseTool
{
    private readonly IIdentityApi identityApi;

    public IdentityTools(IIdentityApi identityApi, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
    {
        this.identityApi = identityApi ?? throw new ArgumentNullException(nameof(identityApi));
    }

    private const string RegisterDescription = "Register a user account. Upon successful registration, an email will be sent containing login details. " +
                                               "Please check your inbox for your email and password. It is recommended to change it after first login.";

    private const string ChangePasswordDescription = "Change the user's password. A valid Bearer token, obtained from a successful login, is required.";

    private const string ResetPasswordDescription = "Reset the user's password. A new random password will be generated and emailed to the user.";

    private const string AssignRoleDescription = "Assigns a specified role to a user. The role must exist before assignment.";

    private const string RefreshTokenDescription = "Refresh the user's authentication token. A valid Bearer token is required.";

    

    [McpServerTool, Description(RegisterDescription)]
    public async Task<string> RegisterAsync([Description("Email address to register")] string email)
    {
        try
        {
            var password = PasswordGenerator.Generate(CommonModelConstants.Identity.DefaultPasswordLength);
            var payload = new { email, password, confirmPassword = password };

            var response = await identityApi.RegisterAsync(payload);
            if (response.IsSuccessStatusCode)
            {
                Log.Information("User registered successfully: {Email}", email);
                return "An email has been sent. Please check your inbox to complete the registration.";
            }

            var errorMessage = await GetErrorMessage(response);
            Log.Warning("Failed to register user: {Email}. Error: {Error}", email, errorMessage);
            return errorMessage;
        }
        catch (Exception ex)
        {
            Log.Error("Exception during user registration: {Email}. Error: {Error}", email, ex.Message);
            return $"An error occurred while registering the user: {ex.Message}";
        }
    }

    [McpServerTool, Description(ChangePasswordDescription)]
    public async Task<string> ChangePasswordAsync(
        [Description("Current password")] string currentPassword,
        [Description("New password to set")] string newPassword)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return token;

            var payload = new { currentPassword, newPassword };
            var response = await identityApi.ChangePasswordAsync(payload, GetBearerToken());

            if (response.IsSuccessStatusCode)
            {
                Log.Information("Password changed successfully.");
                return "Password changed successfully.";
            }

            var errorMessage = await GetErrorMessage(response);
            Log.Warning("Failed to change password. Error: {Error}", errorMessage);
            return errorMessage;
        }
        catch (Exception ex)
        {
            Log.Error("Exception during password change. Error: {Error}", ex.Message);
            return $"An error occurred while changing the password: {ex.Message}";
        }
    }

    [McpServerTool, Description(ResetPasswordDescription)]
    public async Task<string> ResetPasswordAsync([Description("Email address to reset password for")] string email)
    {
        try
        {
            var response = await identityApi.ResetPasswordAsync(new { email });

            if (response.IsSuccessStatusCode)
            {
                Log.Information("Password reset successfully for {Email}", email);
                return "Password has been reset. A new password has been sent to your email.";
            }

            var errorMessage = await GetErrorMessage(response);
            Log.Warning("Failed to reset password for {Email}. Error: {Error}", email, errorMessage);
            return errorMessage;
        }
        catch (Exception ex)
        {
            Log.Error("Exception during password reset for {Email}. Error: {Error}", email, ex.Message);
            return $"An error occurred while resetting the password: {ex.Message}";
        }
    }

    [McpServerTool, Description(AssignRoleDescription)]
    public async Task<string> AssignRoleAsync(
        [Description("User's email address")] string email,
        [Description("Role name to assign")] string roleName)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return token;

            var payload = new { email, roleName };
            var response = await identityApi.AssignRoleAsync(payload, GetBearerToken());

            if (response.IsSuccessStatusCode)
            {
                Log.Information("Role '{RoleName}' successfully assigned to user '{Email}'.", roleName, email);
                return $"Role '{roleName}' successfully assigned to user '{email}'.";
            }

            var errorMessage = await GetErrorMessage(response);
            Log.Warning("Failed to assign role '{RoleName}' to user '{Email}'. Error: {Error}", roleName, email, errorMessage);
            return errorMessage;
        }
        catch (Exception ex)
        {
            Log.Error("Exception during role assignment for {Email}. Error: {Error}", email, ex.Message);
            return $"An error occurred while assigning the role: {ex.Message}";
        }
    }

    [McpServerTool, Description(RefreshTokenDescription)]
    public async Task<string> RefreshTokenAsync(
        [Description("User ID associated with the refresh token")] string userId,
        [Description("Refresh token to renew authentication")] string refreshToken)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return token;

            var payload = new { userId, refreshToken };
            var response = await identityApi.RefreshTokenAsync(payload, token);

            if (response.IsSuccessStatusCode)
            {
                Log.Information("Token refreshed successfully for UserId: {UserId}", userId);
                return "Token refreshed successfully.";
            }

            var errorMessage = await GetErrorMessage(response);
            Log.Warning("Failed to refresh token for UserId: {UserId}. Error: {Error}", userId, errorMessage);
            return errorMessage;
        }
        catch (Exception ex)
        {
            Log.Error("Exception occurred while refreshing token for UserId: {UserId}. Error: {Error}", userId, ex.Message);
            return $"An error occurred while refreshing the token: {ex.Message}";
        }
    }

    
}
