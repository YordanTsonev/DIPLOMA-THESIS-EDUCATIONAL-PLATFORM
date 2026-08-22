using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace EduPlatform.BuildingBlocks.Infrastructure.Seeding;

/// <summary>
/// Runs every registered <see cref="IDataSeeder"/> in <see cref="IDataSeeder.Order"/> order.
/// <para>
/// Seeding is never triggered by a normal application start. It runs only when the host is
/// launched with the <c>--seed</c> argument, so a deployment can never rewrite real data by
/// accident.
/// </para>
/// </summary>
public sealed partial class DataSeedRunner(IEnumerable<IDataSeeder> seeders, ILogger<DataSeedRunner> logger)
{
    /// <summary>The command-line argument that requests seeding.</summary>
    public const string CommandLineFlag = "--seed";

    /// <summary>Number of seeders that ran during the last <see cref="RunAsync"/> call.</summary>
    public int LastRunCount { get; private set; }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var ordered = seeders.OrderBy(seeder => seeder.Order).ToList();
        LastRunCount = ordered.Count;

        if (ordered.Count == 0)
        {
            // Expected until Phase 1: the mechanism is wired, the modules simply have no
            // entities to populate yet.
            LogNothingToSeed();
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        LogSeedingStarted(ordered.Count);

        foreach (var seeder in ordered)
        {
            var step = Stopwatch.StartNew();
            await seeder.SeedAsync(cancellationToken).ConfigureAwait(false);
            LogSeederFinished(seeder.Name, step.ElapsedMilliseconds);
        }

        LogSeedingFinished(stopwatch.ElapsedMilliseconds);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "No data seeders are registered; nothing to seed.")]
    private partial void LogNothingToSeed();

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeding {SeederCount} module(s).")]
    private partial void LogSeedingStarted(int seederCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded {SeederName} in {ElapsedMilliseconds} ms.")]
    private partial void LogSeederFinished(string seederName, long elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeding finished in {ElapsedMilliseconds} ms.")]
    private partial void LogSeedingFinished(long elapsedMilliseconds);
}
