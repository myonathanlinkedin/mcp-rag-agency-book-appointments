using Nest;
using Newtonsoft.Json.Linq;

public class AppointmentKafkaConsumer : BaseKafkaConsumer<string>, IKafkaConsumerService
{
    private readonly IElasticClient elasticClient;
    private readonly string indexName;

    public AppointmentKafkaConsumer(
        string bootstrapServers,
        string groupId,
        IElasticClient elasticClient,
        string indexName
    ) : base(bootstrapServers, groupId)
    {
        this.elasticClient = elasticClient;
        this.indexName = indexName;
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
                case "INSERT":
                    await InsertToElasticAsync(jsonData);
                    break;
                case "UPDATE":
                    await UpdateInElasticAsync(id, jsonData);
                    break;
                case "DELETE":
                    await DeleteFromElasticAsync(id);
                    break;
                default:
                    Console.WriteLine($"Unknown action type: {action}. Skipping.");
                    return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
            return false;
        }
    }

    private async Task InsertToElasticAsync(JObject jsonData)
    {
        await elasticClient.IndexDocumentAsync(jsonData);
        Console.WriteLine($"Inserted document with ID '{jsonData["Id"]}' into ElasticSearch.");
    }

    private async Task UpdateInElasticAsync(string id, JObject jsonData)
    {
        await elasticClient.UpdateAsync<object>(id, u => u.Index(indexName).Doc(jsonData));
        Console.WriteLine($"Updated document with ID '{id}' in ElasticSearch.");
    }

    private async Task DeleteFromElasticAsync(string id)
    {
        await elasticClient.DeleteAsync<object>(id, d => d.Index(indexName));
        Console.WriteLine($"Deleted document with ID '{id}' from ElasticSearch.");
    }
}
