using System.Reflection;
using EduPlatform.BuildingBlocks.Application.Behaviours;
using EduPlatform.BuildingBlocks.Application.Messaging;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EduPlatform.BuildingBlocks.Application;

public static class ApplicationServiceCollectionExtensions
{
    private static readonly Type[] HandlerInterfaces =
    [
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
    ];

    /// <summary>
    /// Registers the dispatcher and the default behaviour pipeline.
    /// Order matters: logging wraps validation, so a rejected message is still logged.
    /// </summary>
    public static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        services.TryAddScoped<IDispatcher, Dispatcher>();

        services.AddScoped(typeof(IPipelineBehaviour<,>), typeof(LoggingBehaviour<,>));
        services.AddScoped(typeof(IPipelineBehaviour<,>), typeof(ValidationBehaviour<,>));

        return services;
    }

    /// <summary>
    /// Registers every command handler, query handler and FluentValidation validator
    /// found in <paramref name="assembly"/>. Each module calls this for its own
    /// Application assembly during start-up.
    /// </summary>
    public static IServiceCollection AddHandlersFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var concreteTypes = assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false });

        foreach (var type in concreteTypes)
        {
            foreach (var contract in type.GetInterfaces().Where(IsHandlerInterface))
            {
                services.AddScoped(contract, type);
            }
        }

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }

    private static bool IsHandlerInterface(Type candidate) =>
        candidate.IsGenericType && HandlerInterfaces.Contains(candidate.GetGenericTypeDefinition());
}
