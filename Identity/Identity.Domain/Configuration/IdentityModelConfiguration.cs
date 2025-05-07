using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

public static class IdentityModelConfiguration
{
    public static IServiceCollection AddIdentityModelConfiguration(
        this IServiceCollection services) => services.AddCommonApplication(Assembly.GetExecutingAssembly());
}
