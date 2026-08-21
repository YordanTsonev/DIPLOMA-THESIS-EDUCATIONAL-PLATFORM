using EduPlatform.BuildingBlocks.Application.Abstractions;
using EduPlatform.BuildingBlocks.Infrastructure.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EduPlatform.BuildingBlocks.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services)
    {
        services.TryAddSingleton<IClock, SystemClock>();
        return services;
    }
}
