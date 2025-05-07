using ModelContextProtocol.Server;
using Serilog;
using System.ComponentModel;

[McpServerToolType]
public sealed class IdentityTools
{
    private const string RegisterDescription = "Register a user account. Upon successful registration, an email will be sent containing your login details. " +
                                               "Please check your inbox for your email address and password. The password is provided for your convenience; " +
                                               "it is recommended that you change it after your first login.";
   
    private const string LoginDescription = "This system allows direct login via chat by prompting the user for their " +
                                             "email and password. Upon successful login, a Bearer token will be returned for authentication purposes. " +
                                             "Do not mention login or re-login after the user has logged in or received the token.";
    
    private const string ChangePasswordDescription = "Change the user's password. A valid Bearer token, obtained from a successful login, is required. " +
                                                     "After a successful password change, do not expose the token. " +
                                                     "Briefly explain what the system has done. Advise the user to log in or re-login before proceeding if the password change does not work.";
    
    private const string ResetPasswordDescription = "Reset the user's password. A new random password will be generated and emailed to the user. " +
                                                    "Advise the user to log in or re-login before proceeding if the password reset does not work.";

    private const string AssignRoleDescription = "Assigns a specified role to a user. The role must exist before assignment. " +
                                                 "Advise the user to log in or re-login before proceeding if the role assignment does not work.";


    private readonly IIdentityApi identityApi;

    public IdentityTools(IIdentityApi identityApi)
    {
        this.identityApi = identityApi;
    }

    [McpServerTool, Description(RegisterDescription)]
    public async Task<string> RegisterAsync([Description("Email address to register")] string email)
    {
        var password = PasswordGenerator.Generate(6);
        var payload = new
        {
            email,
            password,
            confirmPassword = password
        };

        try
        {
            var response = await identityApi.RegisterAsync(payload);

            if (response.IsSuccessStatusCode)
            {
                Log.Information("Successfully registered user: {Email}", email);
                return "An email has been sent. Please check your inbox to complete the registration.";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Log.Error("Failed to register user: {Email}, StatusCode: {StatusCode}, Error: {Error}",
                    email, response.StatusCode, errorContent);
                return $"Failed to register user. Status code: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An exception occurred while registering user: {Email}", email);
            return "An error occurred during registration.";
        }
    }

    [McpServerTool, Description(LoginDescription)]
    public async Task<string> LoginAsync(
        [Description("Email address to log in")] string email,
        [Description("Password to log in")] string password)
    {
        var payload = new
        {
            email,
            password
        };

        try
        {
            var responseBody = await identityApi.LoginAsync(payload);

            var token = TokenExtractor.ExtractTokenFromResponse(responseBody);

            Log.Information("Successfully logged in user: {Email}", email);
            return $"Login successful. Token: {token}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An exception occurred while logging in user: {Email}", email);
            return "An error occurred during login.";
        }
    }

    [McpServerTool, Description(ChangePasswordDescription)]
    public async Task<string> ChangePasswordAsync(
        [Description("The Bearer token obtained after login for authentication")] string token,
        [Description("Current password")] string currentPassword,
        [Description("New password to set")] string newPassword)
    {
        var payload = new
        {
            currentPassword,
            newPassword
        };

        try
        {
            var response = await identityApi.ChangePasswordAsync(payload, $"Bearer {token}");

            if (response.IsSuccessStatusCode)
            {
                Log.Information("Successfully changed password for user.");
                return "Password changed successfully.";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Log.Error("Failed to change password. StatusCode: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                return $"Failed to change password. Status code: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An exception occurred while changing password.");
            return "An error occurred during password change.";
        }
    }

    [McpServerTool, Description(ResetPasswordDescription)]
    public async Task<string> ResetPasswordAsync([Description("Email address to reset password for")] string email)
    {
        var newPassword = PasswordGenerator.Generate(6);  // Generate a random new password
        var payload = new
        {
            email
        };

        try
        {
            var response = await identityApi.ResetPasswordAsync(payload);

            if (response.IsSuccessStatusCode)
            {
                Log.Information("Successfully reset password for user: {Email}", email);
                return "Password has been reset. A new password has been sent to your email.";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Log.Error("Failed to reset password for user: {Email}, StatusCode: {StatusCode}, Error: {Error}",
                    email, response.StatusCode, errorContent);
                return $"Failed to reset password. Status code: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An exception occurred while resetting password for user: {Email}", email);
            return "An error occurred during password reset.";
        }
    }

    [McpServerTool, Description(AssignRoleDescription)]
    public async Task<string> AssignRoleAsync(
       [Description("The Bearer token obtained after login for authentication")] string token,
       [Description("User's email address")] string email,
       [Description("Role name to assign")] string roleName)
    {
        var payload = new
        {
            email,
            roleName
        };

        try
        {
            var response = await identityApi.AssignRoleAsync(payload, $"Bearer {token}");

            if (response.IsSuccessStatusCode)
            {
                Log.Information("Successfully assigned role {RoleName} to user: {Email}", roleName, email);
                return $"Role '{roleName}' successfully assigned to user '{email}'. " +
                       "Advise the user to log in or re-login before proceeding if the role assignment does not work.";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Log.Error("Failed to assign role {RoleName} to user: {Email}. StatusCode: {StatusCode}, Error: {Error}",
                    roleName, email, response.StatusCode, errorContent);
                return $"Failed to assign role. Status code: {response.StatusCode}. " +
                       "Advise the user to log in or re-login before proceeding if the role assignment does not work.";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An exception occurred while assigning role {RoleName} to user: {Email}", roleName, email);
            return "An error occurred during role assignment. " +
                   "Advise the user to log in or re-login before proceeding if the role assignment does not work.";
        }
    }
}