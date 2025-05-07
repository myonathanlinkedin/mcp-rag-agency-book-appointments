using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

public class ChangePasswordEventHandler : IdentityEmailNotificationHandlerBase<PasswordChangedEvent>
{
    public ChangePasswordEventHandler(
        IEmailSender emailSenderService,
        IMCPServerRequester mcpServerRequester,
        ILogger<ChangePasswordEventHandler> logger)
        : base(emailSenderService, mcpServerRequester, logger) { }

    protected override (string, string, string, string) GetEmailData(PasswordChangedEvent e)
    {
        return (
            e.Email,
            e.NewPassword,
            "✅ Your Password Was Successfully Changed",
            $"""
        This is a notification message only. You are not performing any sensitive action.

        Write a plain-text email using these strict rules:

        1. Begin with a friendly, positive greeting.
        2. Confirm that the user's password has been successfully changed.
        3. Include:
           - Username: [EMAIL]
           - New Password: [PASSWORD]
        4. If the user did not initiate this change, advise them to contact support.
        5. Do **not** use words like "sorry", "issue", or anything negative.
        6. Do **not** provide advice, instructions, or links.
        7. Use plain text only. No HTML or formatting.
        8. Include friendly emojis to keep the tone warm and positive.
        9. Output **only** the message text. No explanations or extra content.
        """
        );
    }

    protected override string GetFooter() => "Thanks for keeping your account secure! 🔐";
}
