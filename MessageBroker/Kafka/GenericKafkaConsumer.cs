using Confluent.Kafka;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class GenericKafkaConsumer : IKafkaConsumerService
{
    private readonly IConsumer<Null, string> consumer;
    private readonly IElasticClient elasticClient;
    private readonly string indexName;

    public GenericKafkaConsumer(string bootstrapServers, string groupId, IElasticClient elasticClient, string indexName)
    {
        this.consumer = CreateConsumer(bootstrapServers, groupId);
        this.elasticClient = elasticClient;
        this.indexName = indexName;
    }

    public async Task ConsumeAsync(string topic, CancellationToken cancellationToken)
    {
        try
        {
            consumer.Subscribe(topic);

            while (!cancellationToken.IsCancellationRequested)
            {
                var cr = consumer.Consume(cancellationToken);
                if (!string.IsNullOrEmpty(cr?.Message?.Value))
                {
                    await ProcessMessageAsync(cr.Message.Value);
                }
            }
        }
        catch (OperationCanceledException)
        {
            consumer.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error consuming messages: {ex.Message}");
        }
    }

    private IConsumer<Null, string> CreateConsumer(string bootstrapServers, string groupId)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        return new ConsumerBuilder<Null, string>(config).Build();
    }

    private async Task ProcessMessageAsync(string message)
    {
        try
        {
            var jsonData = JObject.Parse(message);
            var action = jsonData["Action"]?.ToString();
            var id = jsonData["Id"]?.ToString();

            if (string.IsNullOrEmpty(action) || string.IsNullOrEmpty(id)) return;

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
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
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
