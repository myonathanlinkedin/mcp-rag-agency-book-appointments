using Microsoft.Extensions.DependencyInjection;

public static class WebConfiguration
{
    public static IServiceCollection AddAgencyBookWebComponents(
        this IServiceCollection services)
        => services.AddWebComponents(
            typeof(AgencyBookConfiguration));
}