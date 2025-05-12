using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nest;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;

try
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console(theme: AnsiConsoleTheme.Code) // ✅ Adds color output
        .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
        .CreateLogger();

    await CreateHostBuilder(args).Build().RunAsync();
}
finally
{
    Log.CloseAndFlush();
}

static IHostBuilder CreateHostBuilder(string[] args) =>
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
               var log = sp.GetRequiredService<ILogger<AppointmentKafkaConsumer>>();

               return new AppointmentKafkaConsumer(
                   configuration["Kafka:BootstrapServers"],
                   configuration["Kafka:GroupId"],
                   sp.GetRequiredService<IElasticClient>(),
                   configuration["Elasticsearch:IndexName"],
                   log
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
