using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

public static class InfrastructureConfiguration
{
    public static IServiceCollection AddDBStorage<TDbContext>(
        this IServiceCollection services,
        Assembly assembly, 
        string dbConnection)
        where TDbContext : DbContext
    {
        using var scope = services.BuildServiceProvider().CreateScope();
        var appSettings = scope.ServiceProvider.GetRequiredService<ApplicationSettings>();

        return services
            .AddDatabase<TDbContext>(dbConnection)
            .AddRepositories(assembly);
    }

    public static IServiceCollection AddTokenAuthentication(this IServiceCollection services)
    {
        using var scope = services.BuildServiceProvider().CreateScope();
        var appSettings = scope.ServiceProvider.GetRequiredService<ApplicationSettings>()
            ?? throw new ArgumentNullException(nameof(ApplicationSettings));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = appSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = appSettings.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
                    {
                        using var httpClient = new HttpClient();
                        var jwkJson = httpClient.GetStringAsync(appSettings.Jwt.JwksUrl).GetAwaiter().GetResult();
                        var jwk = JsonSerializer.Deserialize<JsonWebKey>(jwkJson);
                        return new List<JsonWebKey> { jwk };
                    }
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
                        Console.WriteLine($"Token validation failed: {context.Exception.Message}");
                        Console.WriteLine($"JWT Token: {token}");
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddHttpContextAccessor();

        return services;
    }

    public static IServiceCollection AddEventSourcing(this IServiceCollection services)
        => services.AddTransient<IEventDispatcher, EventDispatcher>();

    public static IHttpClientBuilder ConfigureDefaultHttpClientHandler(this IHttpClientBuilder builder)
        => builder
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            })
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);

    private static IServiceCollection AddDatabase<TDbContext>(
        this IServiceCollection services,
        string connectionString)
        where TDbContext : DbContext
        => services
            .AddDbContext<TDbContext>(options => options
                .UseSqlServer(
                    connectionString,
                    sqlOptions => sqlOptions
                        .EnableRetryOnFailure(
                            maxRetryCount: 10,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null)
                        .MigrationsAssembly(typeof(TDbContext).Assembly.FullName)));

    internal static IServiceCollection AddRepositories(this IServiceCollection services, Assembly assembly)
        => services
            .Scan(scan => scan
                .FromAssemblies(assembly)
                .AddClasses(classes => classes
                    .AssignableTo(typeof(IDomainRepository<>))
                    .AssignableTo(typeof(IQueryRepository<>)))
                .AsImplementedInterfaces()
                .WithTransientLifetime());
}