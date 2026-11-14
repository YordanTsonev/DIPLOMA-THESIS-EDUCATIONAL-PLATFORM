using EduPlatform.BuildingBlocks.Application;
using EduPlatform.BuildingBlocks.Infrastructure.Persistence;
using EduPlatform.Modules.Content.Application.Abstractions;
using EduPlatform.Modules.Content.Application.UseCases;
using EduPlatform.Modules.Content.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduPlatform.Modules.Content.Infrastructure;

public static class ContentModule
{
    public static IServiceCollection AddContentModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.AddDbContext<ContentDbContext>((provider, options) => options
            .UseNpgsql(connectionString, npgsql => npgsql
                .MigrationsHistoryTable("__migrations", ContentDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(provider.GetRequiredService<PublishDomainEventsInterceptor>()));

        services.AddHandlersFromAssembly(typeof(ListMaterialsQuery).Assembly);
        services.AddScoped<IContentRepository, ContentRepository>();
        services.AddScoped<IContentReadStore, ContentReadStore>();

        return services;
    }
}
