public class DocumentMetadata
{
    public Guid Id { get; set; }
    public string Url { get; set; }
    public string SourceType { get; set; } // "html" or "pdf"
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime ScrapedAt { get; set; }
}
