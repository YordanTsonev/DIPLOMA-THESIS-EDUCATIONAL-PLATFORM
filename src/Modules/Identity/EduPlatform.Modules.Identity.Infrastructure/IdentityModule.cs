using EduPlatform.BuildingBlocks.Infrastructure.Persistence;
using EduPlatform.BuildingBlocks.Infrastructure.Seeding;
using EduPlatform.Modules.Identity.Application.Abstractions;
using EduPlatform.Modules.Identity.Infrastructure.Persistence;
using EduPlatform.Modules.Identity.Infrastructure.Security;
using EduPlatform.Modules.Identity.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduPlatform.Modules.Identity.Infrastructure;

/// <summary>
/// Single entry point through which the host wires up this module. The API host calls
/// <see cref="AddIdentityModule"/> and knows nothing else about the module's internals.
/// </summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.AddDbContext<IdentityDbContext>((provider, options) => options
            .UseNpgsql(connectionString, npgsql => npgsql
                .MigrationsHistoryTable("__migrations", IdentityDbContext.SchemaName))
            // Maps PascalCase properties onto snake_case columns, so the generated schema
            // reads naturally when queried by hand.
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(provider.GetRequiredService<PublishDomainEventsInterceptor>()));

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IDataSeeder, IdentitySeeder>();

        return services;
    }
}
