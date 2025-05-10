using Confluent.Kafka;

public abstract class BaseKafkaConsumer<T>
{
    private readonly IConsumer<Null, string> consumer;

    protected BaseKafkaConsumer(string bootstrapServers, string groupId)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        consumer = new ConsumerBuilder<Null, string>(config).Build();
    }

    public async Task ConsumeAsync(string topic, CancellationToken cancellationToken)
    {
        consumer.Subscribe(topic);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var cr = consumer.Consume(cancellationToken);

                if (!string.IsNullOrEmpty(cr?.Message?.Value))
                {
                    var processed = await ProcessMessageAsync(cr.Message.Value);

                    if (processed)
                    {
                        try
                        {
                            consumer.Commit(cr);
                        }
                        catch (KafkaException ke)
                        {
                            Console.WriteLine($"Offset commit failed: {ke.Message}");
                        }
                    }
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

    protected abstract Task<bool> ProcessMessageAsync(string message);
}
