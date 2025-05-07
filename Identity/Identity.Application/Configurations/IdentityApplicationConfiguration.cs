using System.Reflection;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class IdentityApplicationConfiguration
{
    public static IServiceCollection AddIdentityApplicationConfiguration(
        this IServiceCollection services) => services.AddCommonApplication(Assembly.GetExecutingAssembly());
}
