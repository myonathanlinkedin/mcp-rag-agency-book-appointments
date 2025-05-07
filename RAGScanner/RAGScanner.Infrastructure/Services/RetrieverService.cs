using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;

public class RetrieverService : IRetrieverService
{
    private readonly ILogger<RetrieverService> logger;
    private readonly QdrantClient qdrantClient;
    private readonly ApplicationSettings appSettings;
    private readonly IEmbeddingService embeddingService;
    private const uint DefaultTopK = 10;

    public RetrieverService(
        ILogger<RetrieverService> logger,
        QdrantClient qdrantClient,
        ApplicationSettings appSettings,
        IEmbeddingService embeddingService)
    {
        this.logger = logger;
        this.qdrantClient = qdrantClient;
        this.appSettings = appSettings;
        this.embeddingService = embeddingService;
    }

    public async Task<List<DocumentVector>> RetrieveAllDocumentsAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving all documents from vector store");

        try
        {
            var searchResult = await qdrantClient.ScrollAsync(appSettings.Qdrant.CollectionName, limit: GetSmartLimit());
            return ConvertToDocumentVectors<RetrievedPoint>(searchResult.Result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all documents.");
            return new List<DocumentVector>(); // ✅ No Result wrapper, returning empty list on failure
        }
    }

    public async Task<List<DocumentVector>> RetrieveDocumentsByQueryAsync(string queryText, CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving documents matching query: {QueryText}", queryText);

        var embeddingVector = await embeddingService.GenerateEmbeddingAsync(queryText, cancellationToken);
        if (embeddingVector == null || embeddingVector.Length == 0)
        {
            logger.LogError("Failed to generate embedding.");
            return new List<DocumentVector>(); // ✅ No Result wrapper
        }

        try
        {
            var searchResult = await qdrantClient.SearchAsync(appSettings.Qdrant.CollectionName, embeddingVector, limit: GetSmartTopK());
            return ConvertToDocumentVectors<ScoredPoint>(searchResult);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during document search.");
            return new List<DocumentVector>(); // ✅ Returning empty list on failure
        }
    }

    private List<DocumentVector> ConvertToDocumentVectors<T>(IReadOnlyList<T> points) where T : class
    {
        return points.Select(point => new DocumentVector
        {
            Metadata = MapMetadata(point),
            Embedding = GetEmbeddingData(point)
        }).ToList();
    }

    private DocumentMetadata MapMetadata<T>(T point) where T : class
    {
        var payload = GetPayloadDictionary(point);
        return new DocumentMetadata
        {
            Id = Guid.TryParse(GetUuid(point), out var guid) ? guid : Guid.Empty,
            Url = payload.TryGetValue("url", out var urlValue) ? urlValue.StringValue : string.Empty,
            SourceType = payload.TryGetValue("sourceType", out var sourceTypeValue) ? sourceTypeValue.StringValue : string.Empty,
            Content = payload.TryGetValue("content", out var contentValue) ? contentValue.StringValue : string.Empty,
            Title = payload.TryGetValue("title", out var titleValue) ? titleValue.StringValue : string.Empty,
            ScrapedAt = payload.TryGetValue("scrapedAt", out var scrapedAtValue) &&
                        DateTime.TryParse(scrapedAtValue.StringValue, out var scrapedAt)
                        ? scrapedAt
                        : default
        };
    }

    private static Dictionary<string, Value> GetPayloadDictionary<T>(T point) where T : class =>
        point switch
        {
            RetrievedPoint retrieved => retrieved.Payload.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            ScoredPoint scored => scored.Payload.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            _ => new Dictionary<string, Value>()
        };

    private static string GetUuid<T>(T point) where T : class =>
        point switch
        {
            RetrievedPoint retrieved => retrieved.Id?.Uuid ?? string.Empty,
            ScoredPoint scored => scored.Id?.Uuid ?? string.Empty,
            _ => string.Empty
        };

    private static float[] GetEmbeddingData<T>(T point) where T : class =>
        point switch
        {
            RetrievedPoint retrieved => retrieved.Vectors?.Vector?.Data?.ToArray() ?? Array.Empty<float>(),
            ScoredPoint scored => scored.Vectors?.Vector?.Data?.ToArray() ?? Array.Empty<float>(),
            _ => Array.Empty<float>()
        };

    private uint GetSmartTopK() => DefaultTopK;
    private uint GetSmartLimit() => DefaultTopK * 10;
}