using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;

public class VectorStoreService : IVectorStoreService
{
    private readonly ILogger<VectorStoreService> logger;
    private readonly QdrantClient qdrantClient;
    private readonly ApplicationSettings appSettings;
    private readonly float similarityThreshold;

    public VectorStoreService(
        ILogger<VectorStoreService> logger,
        QdrantClient qdrantClient,
        ApplicationSettings appSettings)
    {
        this.logger = logger;
        this.qdrantClient = qdrantClient;
        this.appSettings = appSettings;
        similarityThreshold = appSettings.Qdrant.SimilarityThreshold;
    }

    public async Task SaveDocumentAsync(DocumentVector documentVector, int vectorSize)
    {
        await EnsureCollectionExistsAsync(vectorSize);

        if (await IsVectorSimilarAsync(documentVector.Embedding))
        {
            logger.LogInformation("Vector is too similar to an existing one. Skipping save.");
            return;
        }

        await InsertVectorAsync(documentVector);
        logger.LogInformation("Successfully saved vector for URL: {Url}", documentVector.Metadata.Url);
    }

    private async Task EnsureCollectionExistsAsync(int vectorSize)
    {
        try
        {
            var collections = await qdrantClient.ListCollectionsAsync();
            if (!collections.Any(c => c == appSettings.Qdrant.CollectionName))
            {
                logger.LogWarning("Collection '{CollectionName}' not found. Creating it.", appSettings.Qdrant.CollectionName);
                await CreateCollectionAsync(vectorSize);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking collection existence.");
            throw;
        }
    }

    private async Task CreateCollectionAsync(int vectorSize)
    {
        try
        {
            await qdrantClient.CreateCollectionAsync(appSettings.Qdrant.CollectionName, new VectorParams
            {
                Size = (ulong)vectorSize,
                Distance = Distance.Cosine
            });

            logger.LogInformation("Successfully created collection '{CollectionName}' in Qdrant.", appSettings.Qdrant.CollectionName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during collection creation in Qdrant.");
        }
    }

    private async Task<bool> IsVectorSimilarAsync(float[] queryVector)
    {
        try
        {
            var searchResult = await qdrantClient.SearchAsync(appSettings.Qdrant.CollectionName, queryVector, limit: 5);
            return searchResult.Any() && searchResult.First().Score >= similarityThreshold;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching for similar vectors in Qdrant.");
            throw;
        }
    }

    private async Task InsertVectorAsync(DocumentVector documentVector)
    {
        try
        {
            var point = new PointStruct
            {
                Id = new PointId { Uuid = Guid.NewGuid().ToString() },
                Vectors = new Vectors { Vector = new Vector { Data = { documentVector.Embedding } } },
                Payload = { CreatePayload(documentVector.Metadata) }
            };

            await qdrantClient.UpsertAsync(appSettings.Qdrant.CollectionName, new List<PointStruct> { point });
            logger.LogInformation("Successfully saved vector for URL: {Url}", documentVector.Metadata.Url);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save vector to Qdrant.");
            throw;
        }
    }

    private Dictionary<string, Value> CreatePayload(DocumentMetadata metadata) =>
        new Dictionary<string, Value>
        {
            ["url"] = new Value { StringValue = metadata.Url },
            ["sourceType"] = new Value { StringValue = metadata.SourceType },
            ["title"] = new Value { StringValue = metadata.Title },
            ["content"] = new Value { StringValue = metadata.Content },
            ["scrapedAt"] = new Value { StringValue = metadata.ScrapedAt.ToString() }
        };
}