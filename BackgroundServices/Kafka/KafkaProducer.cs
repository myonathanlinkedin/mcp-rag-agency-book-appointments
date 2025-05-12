using Confluent.Kafka;
using System.Threading.Tasks;

public class KafkaProducer : IKafkaProducerService
{
    private readonly IProducer<Null, string> producer;

    public KafkaProducer(string bootstrapServers)
    {
        var config = new ProducerConfig { BootstrapServers = bootstrapServers };
        this.producer = new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task ProduceAsync(string topic, string message)
    {
        await this.producer.ProduceAsync(topic, new Message<Null, string> { Value = message });
    }
}
