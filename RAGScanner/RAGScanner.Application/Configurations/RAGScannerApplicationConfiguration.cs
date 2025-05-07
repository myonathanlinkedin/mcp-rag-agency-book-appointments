using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

public static class RAGScannerApplicationConfiguration
{
    public static IServiceCollection AddRAGScannerApplication(
        this IServiceCollection services) => services.AddCommonApplication(Assembly.GetExecutingAssembly());
}
