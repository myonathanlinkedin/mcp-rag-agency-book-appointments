using Refit;

public interface IRAGApi
{
    [Post("/api/RAGScanner/ScanUrl/ScanUrlAsync")]
    Task<HttpResponseMessage> ScanUrlsAsync([Body] object payload, [Header("Authorization")] string token);

    [Post("/api/RAGScanner/RAGSearch/RAGSearchAsync")]
    Task<List<RAGSearchResult>> RAGSearchAsync([Body] object payload, [Header("Authorization ")] string token);
}