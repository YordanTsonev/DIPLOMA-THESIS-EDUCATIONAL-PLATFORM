using EduPlatform.BuildingBlocks.Application;
using EduPlatform.BuildingBlocks.Infrastructure.Persistence;
using EduPlatform.Modules.Notifications.Application.Abstractions;
using EduPlatform.Modules.Notifications.Application.UseCases;
using EduPlatform.Modules.Notifications.Contracts;
using EduPlatform.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduPlatform.Modules.Notifications.Infrastructure;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.AddDbContext<NotificationsDbContext>((provider, options) => options
            .UseNpgsql(connectionString, npgsql => npgsql
                .MigrationsHistoryTable("__migrations", NotificationsDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(provider.GetRequiredService<PublishDomainEventsInterceptor>()));

        services.AddHandlersFromAssembly(typeof(ListMyNotificationsQuery).Assembly);
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationReadStore, NotificationReadStore>();
        services.AddScoped<INotificationWriter, NotificationWriter>();

        return services;
    }
}
