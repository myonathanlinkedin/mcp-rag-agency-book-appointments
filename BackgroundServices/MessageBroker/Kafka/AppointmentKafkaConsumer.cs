using Nest;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;

public class AppointmentKafkaConsumer : BaseKafkaConsumer<string>, IKafkaConsumerService
{
    private readonly IElasticClient elasticClient;
    private readonly string indexName;
    private readonly ILogger<AppointmentKafkaConsumer> logger;

    public AppointmentKafkaConsumer(
        string bootstrapServers,
        string groupId,
        IElasticClient elasticClient,
        string indexName,
        ILogger<AppointmentKafkaConsumer> logger) 
        : base(bootstrapServers, groupId, logger)
    {
        this.elasticClient = elasticClient;
        this.indexName = indexName;
        this.logger = logger;
    }

    protected override async Task<bool> ProcessMessageAsync(string message)
    {
        try
        {
            var jsonData = JObject.Parse(message);
            var action = jsonData["Action"]?.ToString();
            var id = jsonData["Id"]?.ToString();

            if (string.IsNullOrEmpty(action) || string.IsNullOrEmpty(id)) return false;

            switch (action)
            {
                case CommonModelConstants.KafkaOperation.Insert:
                    await InsertToElasticAsync(jsonData);
                    break;
                case CommonModelConstants.KafkaOperation.Update:
                    await UpdateInElasticAsync(id, jsonData);
                    break;
                case CommonModelConstants.KafkaOperation.Delete:
                    await DeleteFromElasticAsync(id);
                    break;
                default:
                    logger.LogWarning("Unknown action type: {Action}. Skipping.", action);
                    return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message: {Message}", ex.Message);
            return false;
        }
    }

    private async Task InsertToElasticAsync(JObject jsonData)
    {
        await elasticClient.IndexDocumentAsync(jsonData);
        logger.LogInformation("Inserted document with ID '{Id}' into ElasticSearch.", jsonData["Id"]);
    }

    private async Task UpdateInElasticAsync(string id, JObject jsonData)
    {
        await elasticClient.UpdateAsync<object>(id, u => u.Index(indexName).Doc(jsonData));
        logger.LogInformation("Updated document with ID '{Id}' in ElasticSearch.", id);
    }

    private async Task DeleteFromElasticAsync(string id)
    {
        await elasticClient.DeleteAsync<object>(id, d => d.Index(indexName));
        logger.LogInformation("Deleted document with ID '{Id}' from ElasticSearch.", id);
    }
}
