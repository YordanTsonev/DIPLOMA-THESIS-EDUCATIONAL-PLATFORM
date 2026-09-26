using System.Globalization;
using EduPlatform.Api.Authorization;
using EduPlatform.Api.Configuration;
using EduPlatform.Api.Endpoints;
using EduPlatform.Api.Middleware;
using EduPlatform.BuildingBlocks.Application;
using EduPlatform.BuildingBlocks.Events;
using EduPlatform.BuildingBlocks.Infrastructure;
using EduPlatform.BuildingBlocks.Infrastructure.Seeding;
using EduPlatform.Modules.Identity.Infrastructure;
using EduPlatform.Modules.Identity.Infrastructure.Security;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
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
    builder.Services.AddIdentityModule(configuration);

    // ---- Authentication ------------------------------------------------------------
    var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
        ?? throw new InvalidOperationException("The 'Jwt' configuration section is missing.");

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                ValidateLifetime = true,

                // The default five minutes of tolerance would keep a 15-minute token alive for
                // twenty, which defeats the point of a short lifetime.
                ClockSkew = TimeSpan.FromSeconds(30),
                RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            };

            // SignalR (Phase 7) cannot set an Authorization header on the WebSocket handshake,
            // so the token arrives in the query string for hub routes only.
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken)
                        && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                },
            };
        });

    builder.Services.AddAuthorization(options => options.AddEduPlatformPolicies());

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

    // Enums travel as their names, not their numbers. "Teacher" survives a reordering of the
    // enum and is readable in a log or a request the client sends; 2 is neither.
    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

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

    app.UseAuthentication();
    app.UseAuthorization();

    // Probes stay anonymous: Kubernetes has no credentials, and a probe that needed them
    // would report the pod unhealthy for the wrong reason.
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        // No dependency checks: a failing "live" probe means "restart me".
        Predicate = _ => false,
    }).AllowAnonymous();

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    }).AllowAnonymous();

    app.MapSystemEndpoints();
    app.MapAuthEndpoints();
    app.MapUserEndpoints();

    // The fallback authorization policy also covers requests that match no endpoint, which would
    // answer a mistyped URL with 401 instead of 404. This anonymous terminal route restores the
    // honest answer without weakening the policy for endpoints that do exist.
    app.MapFallback(() => Results.Problem(
            title: "Not Found",
            statusCode: StatusCodes.Status404NotFound))
        .AllowAnonymous()
        .ExcludeFromDescription();

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
catch (HostAbortedException)
{
    // Thrown by design when "dotnet ef" builds the host to read the model. Not a failure,
    // and logging it as fatal makes every migration command look like a crash.
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
