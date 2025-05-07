using Microsoft.Extensions.DependencyInjection;

public static class WebConfiguration
{
    public static IServiceCollection AddRAGScannerWebComponents(
        this IServiceCollection services)
        => services.AddWebComponents(
            typeof(RAGScannerApplicationConfiguration));
}