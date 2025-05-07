using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

public static class ApplicationConfiguration
{
    public static IServiceCollection AddCommonApplication(this IServiceCollection services, Assembly assembly)
    {
        return services
            .AddEventHandlers(assembly)
            .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly))
            .AddAutoMapper(assembly)
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestValidationBehavior<,>));
    }

    private static IServiceCollection AddAutoMapper(this IServiceCollection services, Assembly assembly) =>
        services.AddAutoMapper((_, config) => config.AddProfile(new MappingProfile(assembly)), Array.Empty<Assembly>());

    private static IServiceCollection AddEventHandlers(this IServiceCollection services, Assembly assembly) =>
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(IEventHandler<>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime());
}