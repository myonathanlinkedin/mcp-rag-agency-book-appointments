using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

public static class IdentityDomainConfiguration
{
    public static IServiceCollection AddIdentityDomain(
        this IServiceCollection services)
        => services
            .AddCommonDomain(
                Assembly.GetExecutingAssembly());
}