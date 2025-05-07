public class ScrapedDocument
{
    public string Url { get; set; }
    public byte[] ContentBytes { get; set; }
    public string ContentText { get; set; }
    public bool IsPdf { get; set; }
    public DateTime ScrapedAt { get; set; }
}