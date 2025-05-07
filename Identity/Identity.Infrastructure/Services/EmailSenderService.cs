using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public class EmailSenderService : IEmailSender
{
    private readonly ApplicationSettings applicationSettings;

    public EmailSenderService(IConfiguration configuration, ApplicationSettings applicationSettings)
    {
        this.applicationSettings = applicationSettings;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        // Access the MailHog settings through ApplicationSettings
        var mailhogSettings = applicationSettings.MailHog;

        var smtpClient = new SmtpClient(mailhogSettings.SmtpServer)
        {
            Port = mailhogSettings.SmtpPort,
            Credentials = new NetworkCredential("user", "password"), // You can leave this as default for MailHog
            EnableSsl = false
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(mailhogSettings.FromAddress),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        mailMessage.To.Add(toEmail);

        await smtpClient.SendMailAsync(mailMessage);
    }
}
