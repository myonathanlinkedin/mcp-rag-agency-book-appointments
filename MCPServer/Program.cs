using MCP.Server.Tools;
using Refit;
using Serilog;
using Serilog.Extensions.Logging;
using System.Net.Http.Headers;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configure logging with Serilog
ConfigureLogging(builder);

// Build configuration to access values like BaseUrl
var configuration = builder.Configuration;
string serverName = configuration["MCP:ServerName"] ?? "MCP Server";

// Register default HttpClient with shared configuration
RegisterHttpClient(builder, configuration);

// Register Refit clients dynamically from the assembly
RegisterRefitClients(builder, configuration);

// Register services that need HttpClient (like RAGTools, IdentityTools, etc.)
RegisterServices(builder);

// Add MCP server with tools
AddMcpServer(builder);

var app = builder.Build();
app.MapMcp();
app.Run();

Log.CloseAndFlush();

// Method to configure logging with Serilog
void ConfigureLogging(WebApplicationBuilder builder)
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console(Serilog.Events.LogEventLevel.Information)
        .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day,
            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
        .CreateLogger();

    builder.Logging.ClearProviders();
    builder.Logging.AddProvider(new SerilogLoggerProvider(Log.Logger));
}

// Method to register the HttpClient with shared configuration for all API clients
void RegisterHttpClient(WebApplicationBuilder builder, IConfiguration configuration)
{
    // Register the default HttpClient with a shared configuration
    builder.Services.AddHttpClient("MCPClient", client =>
    {
        client.BaseAddress = new Uri(configuration["MCP:BaseUrl"]);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    });
}

// Method to register Refit clients dynamically from the assembly
void RegisterRefitClients(WebApplicationBuilder builder, IConfiguration configuration)
{
    // Get all interfaces in the assembly
    var assembly = Assembly.GetExecutingAssembly();

    // Find all interfaces that are Refit client interfaces
    var interfaceTypes = assembly.GetTypes()
                                 .Where(t => t.IsInterface && t.GetMethods().Any(m => m.GetCustomAttributes(typeof(HttpMethodAttribute), false).Any()));

    // Register each Refit interface dynamically as a Refit client
    foreach (var interfaceType in interfaceTypes)
    {
        builder.Services.AddRefitClient(interfaceType)
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(configuration["MCP:BaseUrl"]);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });
    }
}

// Method to register transient services
void RegisterServices(WebApplicationBuilder builder)
{
    // Register tool services (like RAGTools, IdentityTools)
    builder.Services.AddScoped<RAGTools>();
    builder.Services.AddScoped<IdentityTools>();
}

// Method to add MCP server with tools
void AddMcpServer(WebApplicationBuilder builder)
{
    builder.Services.AddMcpServer()
        .WithHttpTransport()
        .WithTools<IdentityTools>()
        .WithTools<RAGTools>();
}
