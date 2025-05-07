using Confluent.Kafka;
using Nest;
using Newtonsoft.Json;

public class GenericKafkaConsumer : IKafkaConsumerService
{
    private readonly IConsumer<Null, string> consumer;
    private readonly IElasticClient elasticClient;
    private readonly string indexName;
    private readonly IApiService apiService;

    public GenericKafkaConsumer(string bootstrapServers, string groupId, IElasticClient elasticClient, string indexName, IApiService apiService)
    {
        this.consumer = CreateConsumer(bootstrapServers, groupId);
        this.elasticClient = elasticClient;
        this.indexName = indexName;
        this.apiService = apiService;
    }

    public async Task ConsumeAsync(string topic, CancellationToken cancellationToken)
    {
        try
        {
            consumer.Subscribe(topic);

            while (!cancellationToken.IsCancellationRequested)
            {
                var cr = consumer.Consume(cancellationToken);
                await ProcessMessageAsync(cr.Message.Value);
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
            var payload = JsonConvert.DeserializeObject<GenericPayload>(message);
            if (payload == null) return;

            if (payload.Action == "DELETE")
            {
                await HandleDeletionAsync(payload);
            }
            else if (payload.Action == "UPDATE")
            {
                await HandleUpdateAsync(payload);
            }
            else
            {
                await HandleIndexingAsync(payload);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
        }
    }

    private async Task HandleDeletionAsync(GenericPayload payload)
    {
        var searchResponse = await elasticClient.SearchAsync<object>(s => s
            .Index(indexName)
            .Query(q => q.Term(t => t.Field("id").Value(payload.Id)))
        );

        foreach (var doc in searchResponse.Hits)
        {
            await elasticClient.DeleteAsync<object>(doc.Id, d => d.Index(indexName));
            Console.WriteLine($"Deleted document with ID '{doc.Id}'.");
        }
    }

    private async Task HandleUpdateAsync(GenericPayload payload)
    {
        var apiResponse = await apiService.GetItemByIdAsync(payload.Id);
        if (apiResponse.IsSuccessStatusCode)
        {
            await elasticClient.UpdateAsync<object>(payload.Id, u => u.Index(indexName).Doc(apiResponse.Content));
            Console.WriteLine($"Updated document with ID '{payload.Id}'.");
        }
        else
        {
            Console.WriteLine($"Failed to update document '{payload.Id}': API returned {apiResponse.StatusCode}");
        }
    }

    private async Task HandleIndexingAsync(GenericPayload payload)
    {
        var apiResponse = await apiService.GetItemByIdAsync(payload.Id);
        if (apiResponse.IsSuccessStatusCode)
        {
            await elasticClient.IndexDocumentAsync(apiResponse.Content);
            Console.WriteLine($"Indexed document with ID '{payload.Id}'.");
        }
        else
        {
            Console.WriteLine($"Failed to index document '{payload.Id}': API returned {apiResponse.StatusCode}");
        }
    }
}