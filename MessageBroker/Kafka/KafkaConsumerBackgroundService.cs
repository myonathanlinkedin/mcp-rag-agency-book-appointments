using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class KafkaConsumerBackgroundService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaConsumerBackgroundService> _logger;
    private IKafkaConsumerService _consumer;

    public KafkaConsumerBackgroundService(IConfiguration configuration, IKafkaConsumerService consumer, ILogger<KafkaConsumerBackgroundService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        _consumer = consumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _consumer.ConsumeAsync(_configuration["Kafka:Topic"], stoppingToken);
        }
        catch (ConsumeException e)
        {
            _logger.LogError($"Error occurred: {e.Error.Reason}");
        }
    }
}
