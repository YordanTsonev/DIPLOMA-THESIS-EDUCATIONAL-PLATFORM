using System.Globalization;
using EduPlatform.Api.Configuration;
using EduPlatform.Api.Endpoints;
using EduPlatform.Api.Middleware;
using EduPlatform.BuildingBlocks.Application;
using EduPlatform.BuildingBlocks.Events;
using EduPlatform.BuildingBlocks.Infrastructure;
using EduPlatform.BuildingBlocks.Infrastructure.Seeding;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;

// A bootstrap logger so failures during start-up are still recorded. It is replaced by the
// fully configured logger as soon as configuration has been read.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

    var configuration = builder.Configuration;

    // ---- Options -----------------------------------------------------------------
    // Validated on start-up rather than on first use: a missing setting should stop the
    // container from reporting itself healthy, not fail a user's request an hour later.
    builder.Services.AddOptions<CorsOptions>()
        .Bind(configuration.GetSection(CorsOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddOptions<StorageOptions>()
        .Bind(configuration.GetSection(StorageOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    // ---- Shared building blocks --------------------------------------------------
    builder.Services.AddSharedInfrastructure();
    builder.Services.AddMessaging();
    builder.Services.AddDomainEvents();

    // ---- Modules -----------------------------------------------------------------
    // Each module registers itself here as it is implemented (Identity from Phase 1).

    // ---- Web -----------------------------------------------------------------------
    var corsOrigins = configuration
        .GetSection($"{CorsOptions.SectionName}:AllowedOrigins")
        .Get<string[]>() ?? [];

    builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
        .WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        // Required for the refresh-token cookie and for SignalR (Phase 1 and Phase 7).
        .AllowCredentials()
        .WithExposedHeaders(CorrelationIdMiddleware.HeaderName)));

    builder.Services.AddProblemDetails(options =>
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        });

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddOpenApi();

    var redisConnectionString = configuration.GetConnectionString("Redis");
    if (!string.IsNullOrWhiteSpace(redisConnectionString))
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "eduplatform:";
        });
    }

    // ---- Health checks -------------------------------------------------------------
    // "live" answers whether the process is up; "ready" answers whether it can actually
    // serve traffic. Kubernetes needs the two to mean different things, or a momentary
    // database blip restarts every pod at once.
    var storageEndpoint = configuration["Storage:Endpoint"] ?? "localhost:9000";

    builder.Services.AddHealthChecks()
        .AddNpgSql(
            configuration.GetConnectionString("Postgres")!,
            name: "postgres",
            tags: ["ready"])
        .AddRedis(
            redisConnectionString ?? "localhost:6379",
            name: "redis",
            tags: ["ready"])
        .AddUrlGroup(
            new Uri($"http://{storageEndpoint}/minio/health/live"),
            name: "minio",
            tags: ["ready"]);

    var app = builder.Build();

    // ---- Pipeline ------------------------------------------------------------------
    app.UseExceptionHandler();
    app.UseStatusCodePages();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options => options
            .WithTitle("EduPlatform API")
            .WithTheme(ScalarTheme.BluePlanet));
    }
    else
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseCors();

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        // No dependency checks: a failing "live" probe means "restart me".
        Predicate = _ => false,
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    });

    app.MapSystemEndpoints();

    // ---- Seeding -------------------------------------------------------------------
    // "dotnet run --project src/EduPlatform.Api -- --seed" populates the development data set
    // and exits without serving traffic. Never runs on an ordinary start.
    if (args.Contains(DataSeedRunner.CommandLineFlag, StringComparer.Ordinal))
    {
        await using var scope = app.Services.CreateAsyncScope();
        var runner = scope.ServiceProvider.GetRequiredService<DataSeedRunner>();
        await runner.RunAsync().ConfigureAwait(false);
        return 0;
    }

    await app.RunAsync().ConfigureAwait(false);
    return 0;
}
catch (Exception exception)
{
    Log.Fatal(exception, "EduPlatform API terminated unexpectedly during start-up");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}

/// <summary>Exposed so integration tests can drive the real pipeline with WebApplicationFactory.</summary>
public partial class Program;
