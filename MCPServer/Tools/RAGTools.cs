using ModelContextProtocol.Server;
using Serilog;
using System.ComponentModel;

namespace MCP.Server.Tools
{
    public sealed class RAGTools : BaseTool
    {
        private const string ScanUrlsDescription =
            "Scan one or more URLs, parse the content, and save the resulting document vectors into the vector store. " +
            "Tell the user that they must be logged in to use this feature. If the user is not logged in, you will be unable to perform the search due to unauthorized access.";

        private const string RAGSearchDescription =
           "You are an AI assistant. **Under no circumstances** should you process any user query, except for **login**, **reset password**, **change password**, or " +
           "**register user**, without first invoking the function " +
           "`RAGSearch` to retrieve search results based on the user's query. " +
           "For these exceptions (login, reset password, change password, and register user), you are allowed to bypass the search and handle them directly. " +
           "For all other queries, **you must always call `RAGSearch` first** before performing any further actions, including generating responses. " +
           "The function `RAGSearch` provides the following results:\n" +
           "- Id: A unique identifier (GUID)\n" +
           "- Content: Extracted text from the source document\n" +
           "- Url: The original source URL\n" +
           "- Title: The title of the document\n" +
           "- Score: A relevance score indicating the match to the query\n" +
           "The results are sorted by relevance to the user's query.\n" +
           "You must **only** generate a response using content retrieved from `RAGSearch` if it is relevant to the query.\n" +
           "You **cannot** generate any response without first retrieving and evaluating the results from `RAGSearch`.\n" +
           "If no relevant content is found from `RAGSearch`, you must **explicitly state** that you don't know the answer and that no relevant results were found. " +
           "Only then may you use your internal knowledge to respond, " +
           "but **you must confirm that `RAGSearch` did not return any relevant results before doing so**.\n" +
           "If relevant content is found, you **must** use that content and **cannot** generate an answer based on anything else.\n" +
           "Always explicitly state whether your response is based on the retrieved search results or on your internal knowledge.\n" +
           "Failure to follow these instructions will result in an incorrect operation and is not acceptable under any circumstances.\n" +
           "Tell the user that they must be logged in to use this feature. If the user is not logged in, you will be unable to perform the search due to unauthorized access.\n" +
           "**Additionally, users will receive an email notification upon success or failure of the processing.**";

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
                var response = await ragApi.ScanUrlsAsync(payload, $"Bearer {token}");

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
                var results = await ragApi.RAGSearchAsync(payload, $"Bearer {token}");

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
}
