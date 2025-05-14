using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

public static class IdentityInfrastructureConfiguration
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
    {
        using var scope = services.BuildServiceProvider().CreateScope();
        var appSettings = scope.ServiceProvider.GetRequiredService<ApplicationSettings>();

        services.AddIdentity(appSettings)
                .AddDBStorage<IdentityDbContext>(Assembly.GetExecutingAssembly(), appSettings.ConnectionStrings.IdentityDBConnection)
                .AddIdentityAssemblyServices();

        // Register RsaKeyProviderService as a singleton, do not modify this.
        services.AddSingleton<IRsaKeyProviderService, RsaKeyProviderService>();

        return services;
    }

    private static IServiceCollection AddIdentity(this IServiceCollection services, ApplicationSettings appSettings)
    {
        var resetTokenExpirationSeconds = appSettings.ResetTokenExpirationSeconds;

        services
            .AddIdentity<User, IdentityRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = CommonModelConstants.Identity.MinPasswordLength;

                options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
                options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
                options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider;
            })
            .AddDefaultTokenProviders()
            .AddEntityFrameworkStores<IdentityDbContext>();

        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromSeconds(resetTokenExpirationSeconds);
        });

        return services;
    }

    private static IServiceCollection AddIdentityAssemblyServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.InNamespaceOf<IdentityService>()) // <- better: typesafe
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}