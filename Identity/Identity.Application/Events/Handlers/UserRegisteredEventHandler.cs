using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

public class UserRegisteredEventHandler : IdentityEmailNotificationHandlerBase<UserRegisteredEvent>
{
    public UserRegisteredEventHandler(
        IEmailSender emailSenderService,
        IMCPServerRequester mcpServerRequester,
        ILogger<UserRegisteredEventHandler> logger)
        : base(emailSenderService, mcpServerRequester, logger) { }

    protected override (string, string, string, string) GetEmailData(UserRegisteredEvent e)
    {
        return (
            e.Email,
            e.Password,
            "🎉 Hooray! Your Account is Ready 🎉",
            $"""
        This is a notification message only. You are not performing any sensitive action.

        Write a plain-text email using these strict rules:

        1. Begin with a cheerful, positive greeting.
        2. Confirm that the user's account has been successfully created.
        3. Include:
           - Username: [EMAIL]
           - Your Password: [PASSWORD]
        4. Do **not** use words like "sorry", "issue", or anything negative.
        5. Do **not** provide advice, instructions, or links.
        6. Use plain text only. No HTML or formatting.
        7. Add friendly emojis to keep the tone upbeat.
        8. Output **only** the message text. No extra content or formatting.
        """
        );
    }

    protected override string GetFooter() => "Welcome aboard! 😊";
}