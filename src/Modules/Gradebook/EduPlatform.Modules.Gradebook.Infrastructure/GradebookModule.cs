using EduPlatform.BuildingBlocks.Application;
using EduPlatform.BuildingBlocks.Infrastructure.Persistence;
using EduPlatform.Modules.Gradebook.Application.Abstractions;
using EduPlatform.Modules.Gradebook.Application.UseCases;
using EduPlatform.Modules.Gradebook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduPlatform.Modules.Gradebook.Infrastructure;

public static class GradebookModule
{
    public static IServiceCollection AddGradebookModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.AddDbContext<GradebookDbContext>((provider, options) => options
            .UseNpgsql(connectionString, npgsql => npgsql
                .MigrationsHistoryTable("__migrations", GradebookDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(provider.GetRequiredService<PublishDomainEventsInterceptor>()));

        services.AddHandlersFromAssembly(typeof(GiveGradeCommand).Assembly);
        services.AddScoped<IGradebookRepository, GradebookRepository>();
        services.AddScoped<IGradebookReadStore, GradebookReadStore>();

        return services;
    }
}
