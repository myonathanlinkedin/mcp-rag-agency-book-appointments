using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

public abstract class IdentityEmailNotificationHandlerBase<TEvent> : IEventHandler<TEvent>
        where TEvent : IDomainEvent
{
    private readonly IEmailSender emailSenderService;
    private readonly IMCPServerRequester mcpServerRequester;
    private readonly ILogger logger;

    protected IdentityEmailNotificationHandlerBase(
        IEmailSender emailSenderService,
        IMCPServerRequester mcpServerRequester,
        ILogger logger)
    {
        this.emailSenderService = emailSenderService;
        this.mcpServerRequester = mcpServerRequester;
        this.logger = logger;
    }

    public async Task Handle(TEvent domainEvent, CancellationToken cancellationToken)
    {
        var (email, password, subject, prompt) = this.GetEmailData(domainEvent);

        this.logger.LogInformation("Requesting email body generation for: {Email}", email);
        var result = await this.mcpServerRequester.RequestAsync(prompt: prompt, cancellationToken: cancellationToken);

        if (!result.Succeeded)
        {
            this.logger.LogError("Failed to generate email body for: {Email}. Reason: {Errors}", email, result.Errors);
            return;
        }

        var body = result.Data.Replace("[EMAIL]", email);
        if (!string.IsNullOrEmpty(password))
            body = body.Replace("[PASSWORD]", password).Replace("[NEW_PASSWORD]", password);

        var fullHtml = $"""
                        <html>
                            <body>
                                <p>{body}</p>
                                <footer><p>{this.GetFooter()}</p></footer>
                            </body>
                        </html>
                        """;

        this.logger.LogInformation("Sending email to {Email}", email);
        await this.emailSenderService.SendEmailAsync(email, subject, fullHtml);
        this.logger.LogInformation("Email successfully sent to {Email}", email);
    }

    protected abstract (string Email, string Password, string Subject, string Prompt) GetEmailData(TEvent domainEvent);
    protected abstract string GetFooter();
}
