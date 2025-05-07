using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

public class PasswordResetEventHandler : IdentityEmailNotificationHandlerBase<PasswordResetEvent>
{
    public PasswordResetEventHandler(
        IEmailSender emailSenderService,
        IMCPServerRequester mcpServerRequester,
        ILogger<PasswordResetEventHandler> logger)
        : base(emailSenderService, mcpServerRequester, logger) { }

    protected override (string, string, string, string) GetEmailData(PasswordResetEvent e)
    {
        return (
            e.Email,
            e.NewPassword,
            "🔒 Your Password Has Been Reset",
            $"""
        This is a notification message only. You are not performing any sensitive action.

        Write a plain-text email using the following strict rules:

        1. Confirm that the user's password has been successfully reset.
        2. Include:
           - Username: [EMAIL]
           - New Password: [PASSWORD]
        3. Do **not** use words like "sorry", "issue", or anything negative.
        4. Do **not** provide advice, instructions, or troubleshooting steps.
        5. Use only plain text. No HTML or formatting.
        6. Include friendly emojis to keep the tone warm and positive.
        7. Output **only** the message text. No explanations, formatting, or metadata.
        """
        );
    }

    protected override string GetFooter() => "Stay secure and take care! 😊";
}