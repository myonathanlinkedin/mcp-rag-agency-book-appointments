using MediatR;
using Microsoft.Extensions.Logging;

public class RAGSearchCommand : IRequest<List<RAGSearchResult>>
{
    public string Query { get; set; } = string.Empty;
    public int TopK { get; set; } = 1; // Number of top documents to retrieve
    public float MinScore { get; set; } = 0.3f; // Minimum similarity score threshold

    public class RAGSearchCommandHandler : IRequestHandler<RAGSearchCommand, List<RAGSearchResult>>
    {
        private readonly IRetrieverService retrieverService;
        private readonly IEmbeddingService embeddingService;
        private readonly ILogger<RAGSearchCommandHandler> logger;
        private const float DefaultMinScore = 0.3f;

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
                // Normalize and clean the query
                var normalizedQuery = NormalizeQuery(request.Query);
                if (string.IsNullOrWhiteSpace(normalizedQuery))
                {
                    logger.LogWarning("Query is empty after normalization");
                    return new List<RAGSearchResult>();
                }

                // 1. Retrieve relevant documents based on the query
                var searchResults = await retrieverService.RetrieveDocumentsByQueryAsync(normalizedQuery, cancellationToken);
                if (!searchResults.Any()) 
                {
                    logger.LogWarning("No documents found for query");
                    return new List<RAGSearchResult>();
                }

                logger.LogInformation("Retrieved {Count} initial documents", searchResults.Count);

                // 2. Apply scoring and filtering
                var minScore = request.MinScore > 0 ? request.MinScore : DefaultMinScore;
                var topResults = searchResults
                    .Select(doc =>
                    {
                        // Normalize Qdrant score from [-1,1] to [0,1] range
                        var normalizedScore = (doc.Score + 1) / 2;
                        
                        // Apply additional relevance boosting based on content
                        var contentBoost = ComputeContentRelevanceBoost(doc.Metadata.Content, normalizedQuery);
                        var titleBoost = ComputeTitleBoost(doc.Metadata.Title, normalizedQuery);
                        
                        var finalScore = (normalizedScore * 0.6f) + (contentBoost * 0.25f) + (titleBoost * 0.15f);

                        return new RAGSearchResult
                        {
                            Id = doc.Metadata.Id,
                            Content = doc.Metadata.Content,
                            Url = doc.Metadata.Url,
                            Title = doc.Metadata.Title,
                            Score = finalScore
                        };
                    })
                    .Where(result => result.Score >= minScore) // Filter by minimum score threshold
                    .OrderByDescending(result => result.Score)
                    .Take(request.TopK)
                    .ToList();

                logger.LogInformation("Returning {Count} results after filtering and scoring", topResults.Count);
                return topResults;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling RAGSearchCommand");
                return new List<RAGSearchResult>();
            }
        }

        private static string NormalizeQuery(string query)
        {
            if (string.IsNullOrEmpty(query)) return string.Empty;
            
            // Remove extra whitespace and convert to lowercase
            return string.Join(" ", 
                query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                .ToLowerInvariant();
        }

        private static float ComputeContentRelevanceBoost(string content, string query)
        {
            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(query))
                return 0f;

            content = content.ToLowerInvariant();
            var queryTerms = query.Split(' ');
            
            // Count how many query terms appear in the content
            var matchCount = queryTerms.Count(term => content.Contains(term));
            
            // Calculate percentage of matching terms
            return (float)matchCount / queryTerms.Length;
        }

        private static float ComputeTitleBoost(string title, string query)
        {
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(query))
                return 0f;

            title = title.ToLowerInvariant();
            
            // Exact match in title gets highest boost
            if (title.Contains(query))
                return 1.0f;
            
            // Partial matches get proportional boost
            var queryTerms = query.Split(' ');
            var matchCount = queryTerms.Count(term => title.Contains(term));
            
            return (float)matchCount / queryTerms.Length;
        }
    }
}