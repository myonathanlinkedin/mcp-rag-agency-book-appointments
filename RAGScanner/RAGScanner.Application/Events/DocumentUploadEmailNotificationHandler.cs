using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

public class DocumentUploadEmailNotificationHandler : RAGEmailNotificationHandlerBase<DocumentScanEvent>
{
    public DocumentUploadEmailNotificationHandler(
        IEmailSender emailSenderService,
        IMCPServerRequester mcpServerRequester,
        ILogger<DocumentUploadEmailNotificationHandler> logger)
        : base(emailSenderService, mcpServerRequester, logger) { }

    protected override (string, string, string, string) GetEmailData(DocumentScanEvent e)
    {
        var trimmedSnippet = e.ContentSnippet?
            .Split(" ")
            .Take(10)
            .Aggregate((a, b) => $"{a} {b}") + "...";

        return (
            e.UploaderEmail,
            "noreply@service.com",
            $"📄 Document Scan Status: {e.Status}",
            $"""
        This is a notification message only. You are not performing any sensitive action.

        Write a plain-text email using these strict rules:

        1. Begin with a friendly, positive greeting.
        2. Confirm the document scan has been completed.
        3. Include:
           - **Document URL:** {e.DocumentUrl}
           - **Scan Time:** {e.ScanTimestamp}
           - **Uploaded by:** {e.UploaderEmail}
           - **Scan Status:** {e.Status} ✅ Added status here
           {(e.PageNumber.HasValue ? $"- **Page Number:** {e.PageNumber}" : "")} ✅ Show page if applicable
           - **Content Snippet:** "{trimmedSnippet}" ✅ Trimmed to max 10 words
        4. If the user did not initiate this scan, advise them to contact support.
        5. Do **not** use words like "sorry", "issue", or anything negative.
        6. Do **not** provide advice, instructions, or links.
        7. Use plain text only. No HTML or formatting.
        8. Include friendly emojis to keep the tone warm and positive.
        9. Output **only** the message text. No explanations or extra content.
        """
        );
    }

    protected override string GetFooter() => "Thanks for using our document scanning service! 🔍";
}