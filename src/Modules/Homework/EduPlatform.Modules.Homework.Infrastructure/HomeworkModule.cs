using EduPlatform.BuildingBlocks.Application;
using EduPlatform.BuildingBlocks.Infrastructure.Persistence;
using EduPlatform.Modules.Homework.Application.Abstractions;
using EduPlatform.Modules.Homework.Application.UseCases;
using EduPlatform.Modules.Homework.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduPlatform.Modules.Homework.Infrastructure;

public static class HomeworkModule
{
    public static IServiceCollection AddHomeworkModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.AddDbContext<HomeworkDbContext>((provider, options) => options
            .UseNpgsql(connectionString, npgsql => npgsql
                .MigrationsHistoryTable("__migrations", HomeworkDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(provider.GetRequiredService<PublishDomainEventsInterceptor>()));

        services.AddHandlersFromAssembly(typeof(CreateAssignmentCommand).Assembly);
        services.AddScoped<IHomeworkRepository, HomeworkRepository>();
        services.AddScoped<IHomeworkReadStore, HomeworkReadStore>();

        return services;
    }
}
