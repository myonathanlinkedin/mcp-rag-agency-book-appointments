using MediatR;
using Microsoft.Extensions.Logging;

public class RAGSearchCommand : IRequest<List<RAGSearchResult>>
{
    public string Query { get; set; } = string.Empty;
    public int TopK { get; set; } = 1; // Number of top documents to retrieve

    public class RAGSearchCommandHandler : IRequestHandler<RAGSearchCommand, List<RAGSearchResult>>
    {
        private readonly IRetrieverService retrieverService;
        private readonly IEmbeddingService embeddingService;
        private readonly ILogger<RAGSearchCommandHandler> logger;

        public RAGSearchCommandHandler(
            IRetrieverService retrieverService,
            IEmbeddingService embeddingService,
            ILogger<RAGSearchCommandHandler> logger)
        {
            this.retrieverService = retrieverService;
            this.embeddingService = embeddingService;
            this.logger = logger;
        }

        public async Task<List<RAGSearchResult>> Handle(RAGSearchCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Handling RAGSearchCommand for query: {Query}, TopK: {TopK}", request.Query, request.TopK);

            try
            {
                // 1. Retrieve relevant documents based on the query
                var searchResults = await retrieverService.RetrieveDocumentsByQueryAsync(request.Query, cancellationToken);
                if (!searchResults.Any()) 
                {
                    logger.LogWarning("No documents found for query.");
                    return new List<RAGSearchResult>(); 
                }

                logger.LogInformation("Successfully retrieved {Count} documents", searchResults.Count);

                // 2. Get the embedding for the query
                var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(request.Query, cancellationToken);
                if (queryEmbedding == null || queryEmbedding.Length == 0)
                {
                    logger.LogError("Failed to generate embedding.");
                    return new List<RAGSearchResult>(); 
                }

                logger.LogInformation("Generated embedding for the query");

                // 3. Compute similarity, map to RAGSearchResult, sort, and take top K
                var topResults = searchResults
                    .Select(doc =>
                    {
                        try
                        {
                            var similarityScore = VectorUtility.ComputeCosineSimilarity(queryEmbedding, doc.Embedding);
                            return new RAGSearchResult
                            {
                                Id = doc.Metadata.Id,
                                Content = doc.Metadata.Content,
                                Url = doc.Metadata.Url,
                                Title = doc.Metadata.Title,
                                Score = similarityScore
                            };
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error mapping document to RAGSearchResult");
                            return null; 
                        }
                    })
                    .Where(result => result != null) // ✅ Filter out invalid results
                    .OrderByDescending(result => result.Score) // ✅ Sort by similarity
                    .Take(request.TopK) // ✅ Take only the top K results
                    .ToList(); // ✅ Materialize execution

                logger.LogInformation("Returning top {TopK} results", request.TopK);
                return topResults; 
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling RAGSearchCommand");
                return new List<RAGSearchResult>(); 
            }
        }
    }
}