using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

public static class AgencyBookInfrastructureConfiguration
{
    public static IServiceCollection AddAgencyBookInfrastructureConfiguration(this IServiceCollection services)
    {
        using var scope = services.BuildServiceProvider().CreateScope();
        var appSettings = scope.ServiceProvider.GetRequiredService<ApplicationSettings>();

        services.AddDBStorage<AgencyBookingDbContext>(
                Assembly.GetExecutingAssembly(),
                appSettings.ConnectionStrings.AgencyBookDBConnection).AddRAGScannerAssemblyServices();

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
}