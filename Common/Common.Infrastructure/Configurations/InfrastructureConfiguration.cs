using Marten;
using Marten.Events.Daemon.Resiliency;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text.Json;

public static class InfrastructureConfiguration
{
    public static IServiceCollection AddDBStorage<TDbContext>(
        this IServiceCollection services,
        Assembly assembly,
        string dbConnection)
        where TDbContext : DbContext
    {
        services.AddStackExchangeRedisCache(options =>
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var settings = GetAppSettings(scope.ServiceProvider);
            options.Configuration = settings.Redis.ConnectionString;
            options.InstanceName = settings.Redis.InstanceName;
        });

        return services
            .AddDatabase<TDbContext>(dbConnection)
            .AddRepositories(assembly);
    }

    public static IServiceCollection AddTokenAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                using var scope = services.BuildServiceProvider().CreateScope();
                var settings = GetAppSettings(scope.ServiceProvider);
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<JwtBearerEvents>>(); // Get logger from DI container

                options.UseSecurityTokenValidators = true;
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeyResolver = (token, secToken, kid, param) =>
                    {
                        var jwkJson = new HttpClient()
                            .GetStringAsync(settings.Jwt.JwksUrl)
                            .GetAwaiter().GetResult();

                        var jwk = JsonSerializer.Deserialize<JsonWebKey>(jwkJson);
                        return new List<JsonWebKey> { jwk };
                    }
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
                        logger.LogError(context.Exception, "Token validation failed: {Message}", context.Exception.Message);
                        logger.LogDebug("JWT Token: {Token}", token);
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    public static IServiceCollection AddEventSourcing(this IServiceCollection services)
    {
        services.AddMarten(options =>
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var settings = GetAppSettings(scope.ServiceProvider);

            options.Connection(settings.ConnectionStrings.EventSourcingConnection);
            options.Events.AddEventType(typeof(IDomainEvent));
            options.Schema.For<IDomainEvent>().Identity(x => x.AggregateId);
        }).AddAsyncDaemon(DaemonMode.HotCold);

        return services
            .AddTransient<IEventDispatcher, EventDispatcher>()
            .AddScoped<IEventRepository, MartenEventRepository>();
    }

    public static IHttpClientBuilder ConfigureDefaultHttpClientHandler(this IHttpClientBuilder builder) =>
        builder
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            })
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);

    private static ApplicationSettings GetAppSettings(IServiceProvider serviceProvider)
        => serviceProvider.GetRequiredService<ApplicationSettings>();

    private static IServiceCollection AddDatabase<TDbContext>(
        this IServiceCollection services,
        string connectionString)
        where TDbContext : DbContext =>
        services.AddDbContext<TDbContext>(options => options
            .UseSqlServer(connectionString, sql => sql
                .EnableRetryOnFailure(10, TimeSpan.FromSeconds(30), null)
                .MigrationsAssembly(typeof(TDbContext).Assembly.FullName)));

    private static IServiceCollection AddRepositories(this IServiceCollection services, Assembly assembly) =>
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes
                .AssignableTo(typeof(IDomainRepository<>))
                .AssignableTo(typeof(IQueryRepository<>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime());
}
