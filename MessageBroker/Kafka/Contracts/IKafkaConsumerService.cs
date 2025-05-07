public interface IKafkaConsumerService
{
    Task ConsumeAsync(string topic, CancellationToken cancellationToken);
}