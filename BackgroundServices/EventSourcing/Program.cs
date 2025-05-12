using Marten;
using Marten.Events.Daemon.Resiliency;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;
using System.Reflection;

Log.Logger = new LoggerConfiguration()
      .WriteTo.Console(theme: AnsiConsoleTheme.Code) // ✅ Adds color output
      .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
      .CreateLogger();

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        // Ensure user secrets are loaded
        if (hostingContext.HostingEnvironment.IsDevelopment())
        {
            config.AddUserSecrets<Program>(optional: false); // This forces loading user secrets
        }

        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;
        var connectionString = configuration["ConnectionStrings:EventSourcingConnection"];

        services.AddMarten(options =>
        {
            options.Connection(connectionString);

            // Enable event store (optional)
            options.Events.AddEventType(typeof(IDomainEvent));

            // Enable document storage
            options.Schema.For<IDomainEvent>().Identity(x => x.AggregateId);

        }).AddAsyncDaemon(DaemonMode.HotCold);

        services.AddTransient<IEventDispatcher, EventDispatcher>()
                   .AddScoped<IEventRepository, MartenEventRepository>();
      
        services.Scan(scan => scan
           .FromAssemblies(Assembly.GetExecutingAssembly())
           .AddClasses(classes => classes
               .AssignableTo(typeof(IDomainRepository<>))
               .AssignableTo(typeof(IQueryRepository<>)))
           .AsImplementedInterfaces()
           .WithTransientLifetime());

        services.AddHostedService<OutboxDispatcherService>();

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddProvider(new SerilogLoggerProvider(Log.Logger));
        });
    })
    .Build();

await host.RunAsync();