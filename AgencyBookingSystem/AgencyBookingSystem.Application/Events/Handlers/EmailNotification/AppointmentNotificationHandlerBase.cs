using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Threading;

public abstract class AppointmentNotificationHandlerBase<TEvent> : IEventHandler<TEvent>
    where TEvent : IDomainEvent
{
    private readonly IEmailSender emailSenderService;
    private readonly IMCPServerRequester mcpServerRequester;
    private readonly ILogger<AppointmentNotificationHandlerBase<TEvent>> logger;

    protected AppointmentNotificationHandlerBase(
        IEmailSender emailSenderService,
        IMCPServerRequester mcpServerRequester,
        ILogger<AppointmentNotificationHandlerBase<TEvent>> logger)
    {
        this.emailSenderService = emailSenderService ?? throw new ArgumentNullException(nameof(emailSenderService));
        this.mcpServerRequester = mcpServerRequester ?? throw new ArgumentNullException(nameof(mcpServerRequester));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(TEvent domainEvent)
    {
        var (email, subject, prompt) = GetEmailData(domainEvent); // Removed Password
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(prompt))
        {
            logger.LogWarning("Invalid email data for appointment notification.");
            return;
        }

        logger.LogInformation("Requesting email body generation for recipient: {RecipientEmail}", email);

        var result = await mcpServerRequester.RequestAsync(prompt: prompt);
        if (result == null || !result.Succeeded)
        {
            logger.LogError("Failed to generate email content for {RecipientEmail}. Errors: {ErrorDetails}", email, result?.Errors);
            return;
        }

        var body = result.Data ?? "Your appointment notification.";
        var fullHtml = $"""
                        <html>
                            <body>
                                <p>{body}</p>
                                <footer><p>{GetFooter()}</p></footer>
                            </body>
                        </html>
                        """;

        logger.LogInformation("Sending appointment notification email to {RecipientEmail}", email);
        await emailSenderService.SendEmailAsync(email, subject, fullHtml);
        logger.LogInformation("Appointment notification email successfully sent to {RecipientEmail}", email);
    }

    protected abstract (string Email, string Subject, string Prompt) GetEmailData(TEvent domainEvent); // Removed Password
    protected abstract string GetFooter();
}
