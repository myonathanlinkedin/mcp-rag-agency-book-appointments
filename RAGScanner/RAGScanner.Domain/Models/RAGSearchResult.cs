public class RAGSearchResult
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public float Score { get; set; }
    public string CleanedContent { get; set; } = string.Empty;  // For debugging OCR cleaning
}

