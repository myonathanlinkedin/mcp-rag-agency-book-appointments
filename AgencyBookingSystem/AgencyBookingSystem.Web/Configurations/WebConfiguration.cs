using Microsoft.Extensions.DependencyInjection;

public static class WebConfiguration
{
    public static IServiceCollection AddAgencyBookingWebComponents(
        this IServiceCollection services)
        => services.AddWebComponents(
            typeof(AgencyBookConfiguration));
}