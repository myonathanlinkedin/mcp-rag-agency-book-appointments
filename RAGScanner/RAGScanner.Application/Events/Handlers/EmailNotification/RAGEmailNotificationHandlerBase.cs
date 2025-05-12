using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

public abstract class RAGEmailNotificationHandlerBase<TEvent> : IEventHandler<TEvent>
    where TEvent : IDomainEvent
{
    private readonly IEmailSender emailSenderService;
    private readonly IMCPServerRequester mcpServerRequester;
    private readonly ILogger<RAGEmailNotificationHandlerBase<TEvent>> logger;

    protected RAGEmailNotificationHandlerBase(
        IEmailSender emailSenderService,
        IMCPServerRequester mcpServerRequester,
        ILogger<RAGEmailNotificationHandlerBase<TEvent>> logger)
    {
        this.emailSenderService = emailSenderService ?? throw new ArgumentNullException(nameof(emailSenderService));
        this.mcpServerRequester = mcpServerRequester ?? throw new ArgumentNullException(nameof(mcpServerRequester));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(TEvent domainEvent, CancellationToken cancellationToken)
    {
        var (email, password, subject, prompt) = GetEmailData(domainEvent);

        logger.LogInformation("Requesting email body generation for: {Email}", email);
        var result = await mcpServerRequester.RequestAsync(prompt: prompt, cancellationToken: cancellationToken);

        if (result == null || !result.Succeeded)
        {
            logger.LogError("Failed to generate email body for: {Email}. Reason: {Errors}", email, result?.Errors);
            return;
        }

        var body = result.Data;

        var fullHtml = $"""
                        <html>
                            <body>
                                <p>{body}</p>
                                <footer><p>{GetFooter()}</p></footer>
                            </body>
                        </html>
                        """;

        logger.LogInformation("Sending email to {Email}", email);
        await emailSenderService.SendEmailAsync(email, subject, fullHtml);
        logger.LogInformation("Email successfully sent to {Email}", email);
    }

    protected abstract (string Email, string Password, string Subject, string Prompt) GetEmailData(TEvent domainEvent);
    protected abstract string GetFooter();
}