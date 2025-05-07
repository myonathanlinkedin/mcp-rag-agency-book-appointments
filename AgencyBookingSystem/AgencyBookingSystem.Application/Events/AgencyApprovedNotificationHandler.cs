using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

public class AgencyApprovedNotificationHandler : AgencyApprovedNotificationBase<AgencyUserAssignedEvent>
{
    public AgencyApprovedNotificationHandler(
        IEmailSender emailSenderService,
        IMCPServerRequester mcpServerRequester,
        ILogger<AgencyApprovedNotificationHandler> logger)
        : base(emailSenderService, mcpServerRequester, logger) { }

    protected override (string Email, string Subject, string Prompt) GetEmailData(AgencyUserAssignedEvent e)
    {
        // Determine if event includes user-specific details or agency-wide approval
        var isUserAssignment = e.UserId.HasValue;

        return isUserAssignment
            ? (
                e.UserEmail!,
                $"🎉 Welcome to {e.AgencyName}",
                $"""
                This is an official confirmation of your agency assignment.

                Write a plain-text email using these strict rules:

                1. Begin with a warm, welcoming greeting.
                2. Confirm that you have been successfully assigned to **{e.AgencyName}**.
                3. Include:
                   - **Your Name:** {e.FullName}
                   - **Agency Name:** {e.AgencyName}
                   - **Roles:** {string.Join(", ", e.Roles!)}
                   - **Agency ID:** {e.AgencyId}
                   - **Agency Email:** {e.AgencyEmail}
                4. Encourage the recipient to get started and reach out if needed.
                5. Keep the tone positive, avoiding terms like "issue" or "problem."
                6. Use **plain text**—avoid bullet points or HTML formatting.
                7. Generate **only** the email content—no additional context or metadata.
                """
            )
            : (
                e.AgencyEmail,
                $"✅ Agency Approval: {e.AgencyName}",
                $"""
                This is an official agency approval notification.

                Write a plain-text email using these strict rules:

                1. Begin with a warm, professional greeting.
                2. Confirm that the agency "{e.AgencyName}" has been successfully approved.
                3. Include:
                   - **Agency Name:** {e.AgencyName}
                   - **Approval Status:** Approved ✅
                   - **Agency ID:** {e.AgencyId}
                   - **Contact Email:** {e.AgencyEmail}
                4. Encourage the agency to begin scheduling appointments.
                5. Keep the tone friendly but professional. 😊
                6. Use **plain text**—avoid bullet points or HTML formatting.
                7. Generate **only** the email content—no extra explanations or metadata.
                """
            );
    }

    protected override string GetFooter() => "Excited to have you onboard! 🎉";
}
