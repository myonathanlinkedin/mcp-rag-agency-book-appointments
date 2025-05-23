﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

public static class AgencyBookConfiguration
{
    public static IServiceCollection AddAgencyBookApplicationConfiguration(
        this IServiceCollection services) => services.AddCommonApplication(Assembly.GetExecutingAssembly());
}
