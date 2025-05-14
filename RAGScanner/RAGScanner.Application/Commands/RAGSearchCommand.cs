using MediatR;
using Microsoft.Extensions.Logging;

public class RAGSearchCommand : IRequest<List<RAGSearchResult>>
{
    public string Query { get; set; } = string.Empty;
    public int TopK { get; set; } = 5; // Number of top documents to retrieve
    public float MinScore { get; set; } = 0.3f; // Minimum similarity score threshold

    public class RAGSearchCommandHandler : IRequestHandler<RAGSearchCommand, List<RAGSearchResult>>
    {
        private readonly IRetrieverService retrieverService;
        private readonly IEmbeddingService embeddingService;
        private readonly ITextCleaningService textCleaningService;
        private readonly ILogger<RAGSearchCommandHandler> logger;
        private const float DefaultMinScore = 0.3f;

        public RAGSearchCommandHandler(
            IRetrieverService retrieverService,
            IEmbeddingService embeddingService,
            ITextCleaningService textCleaningService,
            ILogger<RAGSearchCommandHandler> logger)
        {
            this.retrieverService = retrieverService;
            this.embeddingService = embeddingService;
            this.textCleaningService = textCleaningService;
            this.logger = logger;
        }

        public async Task<List<RAGSearchResult>> Handle(RAGSearchCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Handling RAGSearchCommand for query: {Query}, TopK: {TopK}", request.Query, request.TopK);

            try
            {
                // Clean and normalize the query
                var cleanedQuery = textCleaningService.CleanText(request.Query);
                if (string.IsNullOrWhiteSpace(cleanedQuery))
                {
                    logger.LogWarning("Query is empty after cleaning");
                    return new List<RAGSearchResult>();
                }

                // 1. Retrieve relevant documents based on the cleaned query
                var searchResults = await retrieverService.RetrieveDocumentsByQueryAsync(cleanedQuery, cancellationToken);
                if (!searchResults.Any()) 
                {
                    logger.LogWarning("No documents found for query");
                    return new List<RAGSearchResult>();
                }

                logger.LogInformation("Retrieved {Count} initial documents", searchResults.Count);

                // 2. Apply scoring and filtering with OCR-aware comparison
                var minScore = request.MinScore > 0 ? request.MinScore : DefaultMinScore;
                var topResults = searchResults
                    .Select(doc =>
                    {
                        // Clean the document content for comparison
                        var cleanedContent = textCleaningService.CleanText(doc.Metadata.Content);
                        var cleanedTitle = textCleaningService.CleanText(doc.Metadata.Title);

                        // Normalize Qdrant score from [-1,1] to [0,1] range
                        var normalizedScore = (doc.Score + 1) / 2;
                        
                        // Apply additional relevance boosting based on cleaned content
                        var contentBoost = ComputeContentRelevanceBoost(cleanedContent, cleanedQuery);
                        var titleBoost = ComputeTitleBoost(cleanedTitle, cleanedQuery);
                        
                        // Weighted combination of scores
                        var finalScore = (normalizedScore * 0.5f) +      // Vector similarity
                                       (contentBoost * 0.3f) +           // Content match
                                       (titleBoost * 0.2f);              // Title match

                        return new RAGSearchResult
                        {
                            Id = doc.Metadata.Id,
                            Content = doc.Metadata.Content,  // Keep original content for display
                            Url = doc.Metadata.Url,
                            Title = doc.Metadata.Title,      // Keep original title for display
                            Score = finalScore,
                            CleanedContent = cleanedContent  // Store cleaned version for debugging
                        };
                    })
                    .Where(result => result.Score >= minScore)
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

        private static float ComputeContentRelevanceBoost(string cleanedContent, string cleanedQuery)
        {
            if (string.IsNullOrEmpty(cleanedContent) || string.IsNullOrEmpty(cleanedQuery))
                return 0f;

            var queryTerms = cleanedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (!queryTerms.Any()) return 0f;

            // Count exact matches
            var exactMatchCount = queryTerms.Count(term => 
                cleanedContent.Contains(term, StringComparison.OrdinalIgnoreCase));

            // Calculate fuzzy matches for terms that don't match exactly
            var fuzzyMatchScore = queryTerms
                .Where(term => !cleanedContent.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Select(term => ComputeFuzzyMatchScore(cleanedContent, term))
                .DefaultIfEmpty(0)
                .Average();

            // Combine exact and fuzzy matches
            return (exactMatchCount / (float)queryTerms.Length * 0.7f) + (fuzzyMatchScore * 0.3f);
        }

        private static float ComputeTitleBoost(string cleanedTitle, string cleanedQuery)
        {
            if (string.IsNullOrEmpty(cleanedTitle) || string.IsNullOrEmpty(cleanedQuery))
                return 0f;

            // Exact title match gets highest boost
            if (cleanedTitle.Contains(cleanedQuery, StringComparison.OrdinalIgnoreCase))
                return 1.0f;

            var queryTerms = cleanedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (!queryTerms.Any()) return 0f;

            // Count exact matches in title
            var exactMatchCount = queryTerms.Count(term => 
                cleanedTitle.Contains(term, StringComparison.OrdinalIgnoreCase));

            // Add fuzzy matching for terms that don't match exactly
            var fuzzyMatchScore = queryTerms
                .Where(term => !cleanedTitle.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Select(term => ComputeFuzzyMatchScore(cleanedTitle, term))
                .DefaultIfEmpty(0)
                .Average();

            return (exactMatchCount / (float)queryTerms.Length * 0.8f) + (fuzzyMatchScore * 0.2f);
        }

        private static float ComputeFuzzyMatchScore(string text, string term)
        {
            // Simple character-based similarity for terms that don't match exactly
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.Select(word => ComputeLevenshteinSimilarity(word, term))
                       .DefaultIfEmpty(0)
                       .Max();
        }

        private static float ComputeLevenshteinSimilarity(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0f;

            var distance = ComputeLevenshteinDistance(s1.ToLower(), s2.ToLower());
            var maxLength = Math.Max(s1.Length, s2.Length);
            return 1 - (distance / (float)maxLength);
        }

        private static int ComputeLevenshteinDistance(string s1, string s2)
        {
            var matrix = new int[s1.Length + 1, s2.Length + 1];

            for (var i = 0; i <= s1.Length; i++)
                matrix[i, 0] = i;
            for (var j = 0; j <= s2.Length; j++)
                matrix[0, j] = j;

            for (var i = 1; i <= s1.Length; i++)
            {
                for (var j = 1; j <= s2.Length; j++)
                {
                    var cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[s1.Length, s2.Length];
        }
    }
}