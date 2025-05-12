using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog.Extensions.Logging;
using Confluent.Kafka;
using Nest;
using Refit;
using Microsoft.Extensions.Configuration;

public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code) // ✅ Adds color output
                .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            CreateHostBuilder(args).Build().Run();
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((hostContext, services) =>
            {
                var configuration = hostContext.Configuration;

                services.AddSingleton<IKafkaConsumerService>(sp =>
                {
                    return new AppointmentKafkaConsumer(
                        configuration["Kafka:BootstrapServers"],
                        configuration["Kafka:GroupId"],
                        sp.GetRequiredService<IElasticClient>(),
                        configuration["Elasticsearch:IndexName"]
                    );
                });

                services.AddHostedService<KafkaConsumerBackgroundService>();

                services.AddSingleton<IElasticClient>(sp =>
                {
                    var settings = new ConnectionSettings(new Uri(configuration["Elasticsearch:Url"]))
                        .DefaultIndex(configuration["Elasticsearch:IndexName"]);
                    return new ElasticClient(settings);
                });

                services.AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddProvider(new SerilogLoggerProvider(Log.Logger));
                });
            });
}
