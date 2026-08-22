using EduPlatform.BuildingBlocks.Application.Abstractions;
using EduPlatform.BuildingBlocks.Infrastructure.Seeding;
using EduPlatform.BuildingBlocks.Infrastructure.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EduPlatform.BuildingBlocks.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services)
    {
        services.TryAddSingleton<IClock, SystemClock>();

        // Modules add their own IDataSeeder as they gain entities; the runner picks up whatever
        // is registered, so no module needs to be named here.
        services.TryAddScoped<DataSeedRunner>();

        return services;
    }
}
