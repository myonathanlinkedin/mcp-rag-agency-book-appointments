using ModelContextProtocol.Server;
using Serilog;
using System.ComponentModel;

public sealed class RAGTools : BaseTool
{
    private const string ScanUrlsDescription =
        "Scan and process one or more URLs to extract content and store document vectors in the vector store. " +
        "The content will be semantically searchable after processing is complete.";

    private const string RAGSearchDescription =
       "You are an AI assistant with access to internal document knowledge. For any query, you must:\n" +
       "1. Call `RAGSearch` to retrieve relevant content\n" +
       "2. Use only retrieved content if relevant matches found\n" +
       "3. State if using search results or internal knowledge\n" +
       "4. Never generate responses without searching first\n\n" +
       "Search results include:\n" +
       "- Content: Document text\n" +
       "- Url: Source URL\n" +
       "- Title: Document title\n" +
       "- Score: Relevance score\n\n" +
       "Results are sorted by relevance. Only use internal knowledge if no relevant results found.";

    private readonly IRAGApi ragApi;

    public RAGTools(IRAGApi ragApi, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
    {
        this.ragApi = ragApi ?? throw new ArgumentNullException(nameof(ragApi));
    }

    [McpServerTool, Description(ScanUrlsDescription)]
    public async Task<string> ScanUrlsAsync([Description("List of URLs to scan and process")] List<string> urls)
    {
        var token = GetTokenFromHttpContext();

        if (string.IsNullOrWhiteSpace(token))
        {
            Log.Warning("Authentication token is missing for URL scanning.");
            return "Authentication token is missing or invalid.";
        }

        try
        {
            var payload = new { Urls = urls };
            var response = await ragApi.ScanUrlsAsync(payload, GetBearerToken());

            if (response.IsSuccessStatusCode)
            {
                Log.Information("Successfully scanned URLs: {Urls}", string.Join(", ", urls));
                return "Successfully scanned and processed the URLs.";
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            Log.Error("Failed to scan URLs: {Urls}, StatusCode: {StatusCode}, Error: {Error}",
                string.Join(", ", urls), response.StatusCode, errorContent);
            return $"Failed to scan URLs. Status code: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while scanning URLs: {Urls}", string.Join(", ", urls));
            return "An error occurred while scanning URLs.";
        }
    }

    [McpServerTool, Description(RAGSearchDescription)]
    public async Task<object> RAGSearchAsync([Description("The search query")] string query)
    {
        var token = GetTokenFromHttpContext();

        if (string.IsNullOrWhiteSpace(token))
        {
            Log.Warning("Authentication token is missing for RAG search.");
            return new { Error = "Authentication token is missing or invalid." };
        }

        try
        {
            var payload = new { Query = query };
            var results = await ragApi.RAGSearchAsync(payload, GetBearerToken());

            if (results != null)
            {
                Log.Information("Successfully performed RAG search with query: {Query}", query);
                return results;
            }

            return new { Error = "No results found" };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error during RAG search for query: {Query}", query);
            return new { Error = $"Unexpected error during RAG search: {ex.Message}" };
        }
    }
}
