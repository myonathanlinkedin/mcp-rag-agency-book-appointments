public class DocumentScanEvent : IDomainEvent
{
    public Guid Id { get; } 
    public string DocumentUrl { get; }
    public DateTime ScanTimestamp { get; }
    public string UploaderEmail { get; }
    public string Status { get; }
    public int? PageNumber { get; }
    public string ContentSnippet { get; }

    public Guid AggregateId => Id;

    public DocumentScanEvent(string documentUrl, DateTime scanTimestamp, string uploaderEmail, string status, int? pageNumber, string contentSnippet)
    {
        Id = Guid.NewGuid();

        DocumentUrl = string.IsNullOrWhiteSpace(documentUrl)
            ? throw new ArgumentNullException(nameof(documentUrl))
            : documentUrl;
        ScanTimestamp = scanTimestamp <= DateTime.MinValue
            ? throw new ArgumentException("Invalid scan timestamp", nameof(scanTimestamp))
            : scanTimestamp;
        UploaderEmail = string.IsNullOrWhiteSpace(uploaderEmail)
            ? throw new ArgumentNullException(nameof(uploaderEmail))
            : uploaderEmail;
        Status = string.IsNullOrWhiteSpace(status)
            ? throw new ArgumentNullException(nameof(status))
            : status;
        PageNumber = pageNumber; // Allow null for URLs
        ContentSnippet = string.IsNullOrWhiteSpace(contentSnippet)
            ? throw new ArgumentNullException(nameof(contentSnippet))
            : contentSnippet;
    }
}