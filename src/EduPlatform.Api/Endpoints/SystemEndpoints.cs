using System.Reflection;
using EduPlatform.BuildingBlocks.Application.Abstractions;

namespace EduPlatform.Api.Endpoints;

/// <summary>
/// Diagnostic endpoints. They carry no business meaning — their job is to prove, from the
/// browser, that the client reaches the API and the API reaches its dependencies.
/// </summary>
internal static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system").WithTags("System");

        group.MapGet("/info", (IClock clock, IHostEnvironment environment) => Results.Ok(new SystemInfoResponse(
                Application: "EduPlatform.Api",
                Version: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
                Environment: environment.EnvironmentName,
                ServerTimeUtc: clock.UtcNow)))
            .WithName("GetSystemInfo")
            .WithSummary("Build and environment information for the running API instance.")
            .Produces<SystemInfoResponse>();

        return endpoints;
    }
}

internal sealed record SystemInfoResponse(
    string Application,
    string Version,
    string Environment,
    DateTimeOffset ServerTimeUtc);
