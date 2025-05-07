using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using System.Reflection;

public static class MBConfiguration
{
    public static IServiceCollection AddMBConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMBAssemblyServices();
        services.AddKafkaServices(configuration);
        services.AddElasticsearchClient(configuration);

        return services;
    }

    private static IServiceCollection AddMBAssemblyServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.InNamespaceOf<GenericKafkaConsumer>())
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }

    // ✨ Kafka Extension (Producer Setup)
    private static IServiceCollection AddKafkaServices(this IServiceCollection services, IConfiguration configuration)
    {
        var bootstrapServers = configuration["Kafka:BootstrapServers"];

        services.AddSingleton<IKafkaProducerService>(provider => new KafkaProducer(bootstrapServers));

        return services;
    }

    // ✨ Elasticsearch Extension (Connection Setup)
    private static IServiceCollection AddElasticsearchClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IElasticClient>(provider =>
        {
            var conn = new ConnectionSettings(new Uri(configuration["Elasticsearch:Url"]))
                .DefaultIndex(configuration["Elasticsearch:IndexName"])
                .DisableDirectStreaming();

            return new ElasticClient(conn);
        });

        return services;
    }
}
