using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Vas.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.AddServicesFromAssembly(assembly);

        return services;
    }

    public static IServiceCollection AddServicesFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var implementations = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false }
                && type.Name.EndsWith("Service", StringComparison.Ordinal));

        foreach (var implementation in implementations)
        {
            var contract = implementation.GetInterface($"I{implementation.Name}");
            if (contract is not null)
            {
                services.AddScoped(contract, implementation);
            }
        }

        return services;
    }
}
