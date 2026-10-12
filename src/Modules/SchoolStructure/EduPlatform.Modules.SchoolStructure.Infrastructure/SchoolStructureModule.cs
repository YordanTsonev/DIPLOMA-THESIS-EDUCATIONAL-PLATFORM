using EduPlatform.BuildingBlocks.Application;
using EduPlatform.BuildingBlocks.Infrastructure.Persistence;
using EduPlatform.Modules.SchoolStructure.Application.Abstractions;
using EduPlatform.Modules.SchoolStructure.Application.UseCases;
using EduPlatform.Modules.SchoolStructure.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduPlatform.Modules.SchoolStructure.Infrastructure;

public static class SchoolStructureModule
{
    public static IServiceCollection AddSchoolStructureModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.AddDbContext<SchoolStructureDbContext>((provider, options) => options
            .UseNpgsql(connectionString, npgsql => npgsql
                .MigrationsHistoryTable("__migrations", SchoolStructureDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(provider.GetRequiredService<PublishDomainEventsInterceptor>()));

        services.AddHandlersFromAssembly(typeof(ListSubjectsQuery).Assembly);
        services.AddScoped<ISchoolStructureRepository, SchoolStructureRepository>();
        services.AddScoped<ISchoolStructureReadStore, SchoolStructureReadStore>();

        return services;
    }
}
