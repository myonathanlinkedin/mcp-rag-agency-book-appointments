using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class KafkaConsumerBackgroundService : BackgroundService
{
    private readonly IConfiguration configuration;
    private readonly ILogger<KafkaConsumerBackgroundService> logger;
    private IKafkaConsumerService consumer;

    public KafkaConsumerBackgroundService(IConfiguration configuration, IKafkaConsumerService consumer, ILogger<KafkaConsumerBackgroundService> logger)
    {
        this.configuration = configuration;
        this.logger = logger;
        this.consumer = consumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await this.consumer.ConsumeAsync(this.configuration["Kafka:Topic"], stoppingToken);
        }
        catch (ConsumeException e)
        {
            this.logger.LogError($"Error occurred: {e.Error.Reason}");
        }
    }
}
