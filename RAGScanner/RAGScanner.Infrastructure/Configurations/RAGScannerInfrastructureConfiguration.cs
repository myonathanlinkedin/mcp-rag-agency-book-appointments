using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

public static class RAGScannerInfrastructureConfiguration
{
    public static IServiceCollection AddRAGScannerInfrastructure(
        this IServiceCollection services)
    {
        using var scope = services.BuildServiceProvider().CreateScope();
        var appSettings = scope.ServiceProvider.GetRequiredService<ApplicationSettings>();

        services.AddDBStorage<RAGDbContext>(
                Assembly.GetExecutingAssembly(),
                appSettings.ConnectionStrings.RAGDBConnection)
            .AddRAGScannerAssemblyServices()
            .AddSingleton<ITextCleaningService, TextCleaningService>();

        return services;
    }

    private static IServiceCollection AddRAGScannerAssemblyServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.InNamespaceOf<DocumentParserService>())
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}