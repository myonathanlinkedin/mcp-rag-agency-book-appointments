using ModelContextProtocol.Server;
using Serilog;
using System.ComponentModel;
using Microsoft.AspNetCore.Http;

[McpServerToolType]
public sealed class IdentityTools : BaseTool
{
    private readonly IIdentityApi identityApi;

    public IdentityTools(IIdentityApi identityApi, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
    {
        this.identityApi = identityApi ?? throw new ArgumentNullException(nameof(identityApi));
    }

    private string? GetToken()
    {
        var token = GetTokenFromHttpContext();
        return string.IsNullOrWhiteSpace(token) ? LogAndReturnMissingToken() : $"Bearer {token}";
    }

    private string LogAndReturnMissingToken()
    {
        Log.Warning("Authentication token is missing.");
        return "Authentication token is missing or invalid.";
    }

    private const string RegisterDescription = "Register a user account. Upon successful registration, an email will be sent containing your login details. " +
                                               "Please check your inbox for your email address and password. The password is provided for your convenience; " +
                                               "it is recommended that you change it after your first login.";

    private const string LoginDescription = "This system allows direct login via chat by prompting the user for their email and password. " +
                                             "Upon successful login, a Bearer token will be returned for authentication purposes.";

    private const string ChangePasswordDescription = "Change the user's password. A valid Bearer token, obtained from a successful login, is required.";

    private const string ResetPasswordDescription = "Reset the user's password. A new random password will be generated and emailed to the user.";

    private const string AssignRoleDescription = "Assigns a specified role to a user. The role must exist before assignment.";

    [McpServerTool, Description(RegisterDescription)]
    public async Task<string> RegisterAsync([Description("Email address to register")] string email)
    {
        var password = PasswordGenerator.Generate(CommonModelConstants.Identity.DefaultPasswordLength);
        var payload = new { email, password, confirmPassword = password };

        var response = await identityApi.RegisterAsync(payload);
        return response.IsSuccessStatusCode ? "An email has been sent. Please check your inbox to complete the registration."
            : $"Failed to register user. Status code: {response.StatusCode}";
    }

    [McpServerTool, Description(ChangePasswordDescription)]
    public async Task<string> ChangePasswordAsync(
        [Description("Current password")] string currentPassword,
        [Description("New password to set")] string newPassword)
    {
        var token = GetToken();
        if (token == LogAndReturnMissingToken()) return token;

        var payload = new { currentPassword, newPassword };
        var response = await identityApi.ChangePasswordAsync(payload, token);
        return response.IsSuccessStatusCode ? "Password changed successfully."
            : $"Failed to change password. Status code: {response.StatusCode}";
    }

    [McpServerTool, Description(ResetPasswordDescription)]
    public async Task<string> ResetPasswordAsync([Description("Email address to reset password for")] string email)
    {
        var response = await identityApi.ResetPasswordAsync(new { email });
        return response.IsSuccessStatusCode ? "Password has been reset. A new password has been sent to your email."
            : $"Failed to reset password. Status code: {response.StatusCode}";
    }

    [McpServerTool, Description(AssignRoleDescription)]
    public async Task<string> AssignRoleAsync(
        [Description("User's email address")] string email,
        [Description("Role name to assign")] string roleName)
    {
        var token = GetToken();
        if (token == LogAndReturnMissingToken()) return token;

        var payload = new { email, roleName };
        var response = await identityApi.AssignRoleAsync(payload, token);
        return response.IsSuccessStatusCode ? $"Role '{roleName}' successfully assigned to user '{email}'."
            : $"Failed to assign role. Status code: {response.StatusCode}.";
    }
}
