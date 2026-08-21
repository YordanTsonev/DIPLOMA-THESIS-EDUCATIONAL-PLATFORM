using EduPlatform.BuildingBlocks.Infrastructure;
using EduPlatform.BuildingBlocks.Infrastructure.Seeding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace EduPlatform.Api.IntegrationTests;

/// <summary>
/// The seeding mechanism has no seeders to run until Phase 1, so these tests prove the mechanism
/// itself: modules are discovered through dependency injection, ordering is respected, and an
/// empty registration is a valid state rather than a crash.
/// </summary>
public sealed class DataSeedRunnerTests
{
    [Fact]
    public async Task Runs_seeders_in_declared_order()
    {
        var executionLog = new List<string>();
        var runner = new DataSeedRunner(
            [
                new RecordingSeeder(Order: 30, Name: "gradebook", Log: executionLog),
                new RecordingSeeder(Order: 10, Name: "identity", Log: executionLog),
                new RecordingSeeder(Order: 20, Name: "school", Log: executionLog),
            ],
            NullLogger<DataSeedRunner>.Instance);

        await runner.RunAsync();

        // Students cannot be enrolled before their classes exist, so order is not cosmetic.
        executionLog.ShouldBe(["identity", "school", "gradebook"]);
    }

    [Fact]
    public async Task Succeeds_when_no_module_has_registered_a_seeder()
    {
        var runner = new DataSeedRunner([], NullLogger<DataSeedRunner>.Instance);

        await Should.NotThrowAsync(() => runner.RunAsync());

        runner.LastRunCount.ShouldBe(0);
    }

    [Fact]
    public void Runner_is_resolvable_from_the_shared_infrastructure_registration()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSharedInfrastructure();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // A module that adds an IDataSeeder in Phase 1 needs no further wiring than this.
        scope.ServiceProvider.GetRequiredService<DataSeedRunner>().ShouldNotBeNull();
    }

    [Fact]
    public async Task Discovers_seeders_registered_by_modules()
    {
        var executionLog = new List<string>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSharedInfrastructure();
        services.AddScoped<IDataSeeder>(_ => new RecordingSeeder(1, "identity", executionLog));
        services.AddScoped<IDataSeeder>(_ => new RecordingSeeder(2, "school", executionLog));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        await scope.ServiceProvider.GetRequiredService<DataSeedRunner>().RunAsync();

        executionLog.ShouldBe(["identity", "school"]);
    }

    private sealed record RecordingSeeder(int Order, string Name, List<string> Log) : IDataSeeder
    {
        public Task SeedAsync(CancellationToken cancellationToken = default)
        {
            Log.Add(Name);
            return Task.CompletedTask;
        }
    }
}
