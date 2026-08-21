using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace EduPlatform.Api.IntegrationTests;

/// <summary>
/// Drives the real request pipeline through <see cref="WebApplicationFactory{TEntryPoint}"/>,
/// so middleware ordering, routing and serialisation are all exercised — not mocked.
/// </summary>
public sealed class SystemEndpointsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task System_info_reports_the_running_application()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/v1/system/info", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var info = await response.Content.ReadFromJsonAsync<SystemInfo>();
        info.ShouldNotBeNull();
        info.Application.ShouldBe("EduPlatform.Api");
        info.ServerTimeUtc.ShouldBeGreaterThan(DateTimeOffset.UnixEpoch);
    }

    [Fact]
    public async Task Liveness_probe_succeeds_without_touching_dependencies()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Every_response_carries_a_correlation_id()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/v1/system/info", UriKind.Relative));

        response.Headers.TryGetValues("X-Correlation-Id", out var values).ShouldBeTrue();
        values!.Single().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task A_supplied_correlation_id_is_preserved()
    {
        using var client = factory.CreateClient();
        const string Supplied = "0199c0de-cafe-7000-8000-abcdefabcdef";

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/v1/system/info", UriKind.Relative));
        request.Headers.Add("X-Correlation-Id", Supplied);

        var response = await client.SendAsync(request);

        response.Headers.GetValues("X-Correlation-Id").Single().ShouldBe(Supplied);
    }

    [Fact]
    public async Task An_unknown_route_returns_a_problem_document()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/v1/does-not-exist", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    private sealed record SystemInfo(
        string Application,
        string Version,
        string Environment,
        DateTimeOffset ServerTimeUtc);
}
