using FluentValidation;
using Leyi.Application.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Leyi.Application;

public static class DependencyInjection
{
    private static readonly Type[] HandlerInterfaces =
    [
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
    ];

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHandlers(ApplicationAssembly.Assembly.GetTypes());
        services.AddValidatorsFromAssembly(ApplicationAssembly.Assembly, ServiceLifetime.Singleton, includeInternalTypes: true);

        return services;
    }

    /// <summary>
    /// Registers every concrete command/query handler among <paramref name="candidateTypes"/> as scoped.
    /// Each command/query type must have exactly one handler.
    /// </summary>
    internal static IServiceCollection AddHandlers(this IServiceCollection services, IEnumerable<Type> candidateTypes)
    {
        var registrations = candidateTypes
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(contract => contract.IsGenericType
                    && HandlerInterfaces.Contains(contract.GetGenericTypeDefinition()))
                .Select(contract => (Contract: contract, Implementation: type)));

        foreach (var (contract, implementation) in registrations)
        {
            if (services.Any(descriptor => descriptor.ServiceType == contract))
            {
                throw new InvalidOperationException(
                    $"More than one handler is registered for '{contract}'. Found '{implementation}'.");
            }

            services.AddScoped(contract, implementation);
        }

        return services;
    }
}
