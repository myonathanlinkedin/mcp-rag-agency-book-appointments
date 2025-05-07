using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

public class AgencyRegisteredEventNotificationHandler : AgencyRegisteredEventNotificationBase<AgencyRegisteredEvent>
{
    public AgencyRegisteredEventNotificationHandler(
        IEmailSender emailSenderService,
        IMCPServerRequester mcpServerRequester,
        ILogger<AgencyRegisteredEventNotificationHandler> logger)
        : base(emailSenderService, mcpServerRequester, logger) { }

    protected override (string Email, string Subject, string Prompt) GetEmailData(AgencyRegisteredEvent e)
    {
        return (
            e.AgencyEmail,
            $"🎉 Welcome to {e.AgencyName}",
            $"""
            This is an official confirmation of your agency registration.

            Write a plain-text email using these strict rules:

            1. Begin with a warm, welcoming greeting.
            2. Confirm that your agency "{e.AgencyName}" has been successfully registered.
            3. Include:
               - **Agency Name:** {e.AgencyName}
               - **Approval Required:** {(e.RequiresApproval ? "Yes" : "No")}
               - **Agency ID:** {e.AgencyId}
               - **Agency Email:** {e.AgencyEmail}
            4. Encourage the recipient to begin setting up appointments if applicable.
            5. Keep the tone positive—avoid terms like "issue" or "problem."
            6. Use **plain text**—avoid bullet points or HTML formatting.
            7. Generate **only** the email content—no additional context or metadata.
            """
        );
    }

    protected override string GetFooter() => "Excited to have you onboard! 🎉";
}
