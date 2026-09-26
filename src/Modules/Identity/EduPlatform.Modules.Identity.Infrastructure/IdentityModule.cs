using EduPlatform.BuildingBlocks.Application;
using EduPlatform.BuildingBlocks.Infrastructure.Persistence;
using EduPlatform.BuildingBlocks.Infrastructure.Seeding;
using EduPlatform.Modules.Identity.Application;
using EduPlatform.Modules.Identity.Application.Abstractions;
using EduPlatform.Modules.Identity.Contracts;
using EduPlatform.Modules.Identity.Infrastructure.Mail;
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

        // ---- Options ----------------------------------------------------------------
        services.AddOptions<IdentitySettings>()
            .Bind(configuration.GetSection(IdentitySettings.SectionName));

        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpOptions.SectionName));

        // Validated on start-up: an API running without a signing key would accept nobody,
        // and it should refuse to start rather than fail every sign-in at runtime.
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // ---- Use cases --------------------------------------------------------------
        services.AddHandlersFromAssembly(typeof(IdentitySettings).Assembly);

        // ---- Persistence ------------------------------------------------------------
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserReadStore, UserReadStore>();
        services.AddScoped<IUserDirectory, UserReadStore>();
        services.AddScoped<IAuditLog, AuditLog>();

        // ---- Security ---------------------------------------------------------------
        services.AddHttpContextAccessor();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenHasher, Sha256TokenHasher>();
        services.AddScoped<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddScoped<ICurrentUser, CurrentUser>();

        // ---- Mail -------------------------------------------------------------------
        services.AddScoped<IEmailSender, MailKitEmailSender>();

        services.AddScoped<IDataSeeder, IdentitySeeder>();

        return services;
    }
}
