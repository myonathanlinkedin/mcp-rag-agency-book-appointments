using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

public static class AgencyBookInfrastructureConfiguration
{
    public static IServiceCollection AddAgencyBookInfrastructureConfiguration(this IServiceCollection services)
    {
        using var scope = services.BuildServiceProvider().CreateScope();
        var appSettings = scope.ServiceProvider.GetRequiredService<ApplicationSettings>();

        services.AddDBStorage<AgencyBookingDbContext>(
                Assembly.GetExecutingAssembly(),
                appSettings.ConnectionStrings.AgencyBookDBConnection)
            .AddRAGScannerAssemblyServices();

        services.AddKafka(appSettings); 

        return services;
    }

    private static IServiceCollection AddRAGScannerAssemblyServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.InNamespaceOf<AgencyService>()) // <- better: typesafe
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }

    private static IServiceCollection AddKafka(this IServiceCollection services, ApplicationSettings appSettings)
    {
        var kafkaSettings = appSettings.Kafka; 

        services.AddSingleton<IProducer<Null, string>>(sp =>
        {
            var config = new ProducerConfig
            {
                BootstrapServers = kafkaSettings.BootstrapServers
            };

            return new ProducerBuilder<Null, string>(config).Build();
        });

        services.AddSingleton<IConsumer<Null, string>>(sp =>
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = kafkaSettings.BootstrapServers,
                GroupId = kafkaSettings.GroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            return new ConsumerBuilder<Null, string>(config).Build();
        });

        return services;
    }
}
