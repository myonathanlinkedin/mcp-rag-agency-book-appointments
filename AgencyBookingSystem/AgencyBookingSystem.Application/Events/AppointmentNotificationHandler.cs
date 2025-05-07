using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

public class AppointmentNotificationHandler : AppointmentNotificationHandlerBase<AppointmentEvent>
{
    public AppointmentNotificationHandler(
        IEmailSender emailSenderService,
        IMCPServerRequester mcpServerRequester,
        ILogger<AppointmentNotificationHandler> logger)
        : base(emailSenderService, mcpServerRequester, logger) { }

    protected override (string Email, string Subject, string Prompt) GetEmailData(AppointmentEvent e)
    {
        return (
            e.UserEmail,
            $"📅 Appointment Confirmation: {e.AppointmentName}",
            $"""
            This is an appointment confirmation email. Please review the details carefully.

            Write a plain-text email using these strict rules:

            1. Start with a warm, positive greeting.
            2. Confirm the appointment is successfully scheduled.
            3. Include:
               - **Appointment Name:** {e.AppointmentName}
               - **Date & Time:** {e.AppointmentDate}
               - **Status:** {e.Status} ✅ Appointment status added
               - **Agency Name:** {e.AgencyName}
               - **Agency Email:** {e.AgencyEmail}
               - **Recipient Email:** {e.UserEmail}
            4. If the recipient didn't book this appointment, instruct them to contact support.
            5. Do **not** include negative words like "sorry" or "error."
            6. Do **not** provide instructions, troubleshooting steps, or links.
            7. Use only **plain text**—avoid formatting, bullet points, or HTML.
            8. Add friendly emojis for a warm tone. 😀
            9. Generate **only** the email content—no extra context or explanations.
            """
        );
    }

    protected override string GetFooter() => "Looking forward to seeing you at your appointment! 📅";
}
