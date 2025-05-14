using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Google.Protobuf.Collections;

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
            return new List<DocumentVector>();
        }
    }

    public async Task<List<DocumentVector>> RetrieveDocumentsByQueryAsync(string queryText, CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving documents matching query: {QueryText}", queryText);

        var embeddingVector = await embeddingService.GenerateEmbeddingAsync(queryText, cancellationToken);
        if (embeddingVector == null || embeddingVector.Length == 0)
        {
            logger.LogError("Failed to generate embedding.");
            return new List<DocumentVector>();
        }

        try
        {
            var searchResult = await qdrantClient.SearchAsync(appSettings.Qdrant.CollectionName, embeddingVector, limit: GetSmartTopK());
            return ConvertToDocumentVectors<ScoredPoint>(searchResult);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during document search.");
            return new List<DocumentVector>();
        }
    }

    private List<DocumentVector> ConvertToDocumentVectors<T>(IReadOnlyList<T> points) where T : class
    {
        return points.Select(point => new DocumentVector
        {
            Metadata = MapMetadata(point),
            Embedding = GetEmbeddingData(point),
            Score = GetScore(point)
        }).ToList();
    }

    private static DocumentMetadata MapMetadata(object point) =>
        point switch
        {
            RetrievedPoint retrieved => MapPayloadToMetadata(retrieved.Payload),
            ScoredPoint scored => MapPayloadToMetadata(scored.Payload),
            _ => new DocumentMetadata()
        };

    private static float[] GetEmbeddingData<T>(T point) where T : class =>
        point switch
        {
            RetrievedPoint retrieved => retrieved.Vectors?.Vector?.Data?.ToArray() ?? Array.Empty<float>(),
            ScoredPoint scored => scored.Vectors?.Vector?.Data?.ToArray() ?? Array.Empty<float>(),
            _ => Array.Empty<float>()
        };

    private static float GetScore<T>(T point) where T : class =>
        point switch
        {
            ScoredPoint scored => scored.Score,
            _ => 0f
        };

    private uint GetSmartTopK() => DefaultTopK;
    private uint GetSmartLimit() => DefaultTopK * 10;

    private static DocumentMetadata MapPayloadToMetadata(MapField<string, Value> payload)
    {
        if (payload == null) return new DocumentMetadata();

        return new DocumentMetadata
        {
            Id = GetGuidFromPayload(payload, "id"),
            Content = GetStringFromPayload(payload, "content"),
            Url = GetStringFromPayload(payload, "url"),
            Title = GetStringFromPayload(payload, "title")
        };
    }

    private static string GetStringFromPayload(MapField<string, Value> payload, string key)
    {
        if (payload.TryGetValue(key, out var value))
        {
            return value.StringValue;
        }
        return string.Empty;
    }

    private static Guid GetGuidFromPayload(MapField<string, Value> payload, string key)
    {
        var stringValue = GetStringFromPayload(payload, key);
        return Guid.TryParse(stringValue, out var guid) ? guid : Guid.Empty;
    }
}