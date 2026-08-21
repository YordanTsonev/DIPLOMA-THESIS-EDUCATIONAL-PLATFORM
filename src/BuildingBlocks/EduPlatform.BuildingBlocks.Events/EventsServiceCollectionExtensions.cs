using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EduPlatform.BuildingBlocks.Events;

public static class EventsServiceCollectionExtensions
{
    public static IServiceCollection AddDomainEvents(this IServiceCollection services)
    {
        services.TryAddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        return services;
    }

    /// <summary>
    /// Registers every <see cref="IDomainEventHandler{TEvent}"/> in the assembly.
    /// A module subscribes to another module's events by placing a handler in its own assembly.
    /// </summary>
    public static IServiceCollection AddDomainEventHandlersFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var concreteTypes = assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false });

        foreach (var type in concreteTypes)
        {
            var contracts = type.GetInterfaces().Where(candidate =>
                candidate.IsGenericType
                && candidate.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>));

            foreach (var contract in contracts)
            {
                services.AddScoped(contract, type);
            }
        }

        return services;
    }
}
