using Hangfire;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Microsoft.Extensions.Options;
using Marten;

var builder = WebApplication.CreateBuilder(args);

// Register ApplicationSettings
builder.Services.Configure<ApplicationSettings>(builder.Configuration.GetSection("ApplicationSettings"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<ApplicationSettings>>().Value);

builder.Services
    .AddCommonApplication(Assembly.GetExecutingAssembly())
    .AddIdentityApplicationConfiguration()
    .AddIdentityInfrastructure()
    .AddIdentityWebComponents()
    .AddIdentityModelConfiguration()
    .AddTokenAuthentication()
    .AddRAGScannerApplication()
    .AddRAGScannerInfrastructure()
    .AddRAGScannerWebComponents()
    .AddAgencyBookApplicationConfiguration()
    .AddAgencyBookInfrastructureConfiguration()
    .AddAgencyBookWebComponents()
    .AddEventSourcing()
    .AddModelBinders()
    .AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "Web API", Version = "v1" }))
    .AddHttpClient()
    .AddMcpClient()    
    .AddMemoryCache()    
    .AddSingleton(sp =>
    {
        var appSettings = sp.GetRequiredService<ApplicationSettings>();
        return appSettings.ConnectionStrings.RAGDBConnection;
    })
    .AddHangfire((serviceProvider, config) =>
    {
        var appSettings = serviceProvider.GetRequiredService<ApplicationSettings>();
        var connectionString = appSettings.ConnectionStrings.RAGDBConnection;
        config.UseSqlServerStorage(connectionString);
    })
    .AddHangfireServer()
    .AddSession(options =>
    {
        options.Cookie.Name = ".Prompting.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.IdleTimeout = TimeSpan.FromMinutes(30);
    })
    .AddHttpContextAccessor()
    .AddAuthorization(options =>
    {
        options.AddPolicy(CommonModelConstants.Policy.AdminAccess, policy =>
            policy.RequireRole(CommonModelConstants.Role.Administrator));
    });

builder.Services.AddScoped<QdrantClient>(sp =>
{
    var settings = sp.GetRequiredService<ApplicationSettings>().Qdrant;
    var uri = new Uri(settings.Endpoint);
    var channel = QdrantChannel.ForAddress(uri, new ClientConfiguration
    {
        //ApiKey = settings.ApiKey,
        //CertificateThumbprint = settings.CerCertificateThumbprint
    });

    var grpcClient = new QdrantGrpcClient(channel);
    return new QdrantClient(grpcClient);
});

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

var app = builder.Build();

// Ensure cookies are only sent over HTTPS
app.UseCookiePolicy(new CookiePolicyOptions
{
    Secure = CookieSecurePolicy.Always // Ensures cookies are sent only over HTTPS
});

app.UseHangfireDashboard("/hangfire");

app
    .UseHttpsRedirection()
    .UseSession()
    .UseWebService(app.Environment)
    .Initialize();

app.Run();