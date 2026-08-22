namespace EduPlatform.BuildingBlocks.Infrastructure.Seeding;

/// <summary>
/// Populates one module's tables with the development data set.
/// <para>
/// Every module contributes its own implementation as it gains entities: Identity supplies the
/// users and roles in Phase 1, SchoolStructure the school, classes and enrolments in Phase 2, and
/// so on. Seeders must be <b>idempotent</b> — running one twice against the same database has to
/// leave the same result, because the seed command is expected to be re-run freely.
/// </para>
/// </summary>
public interface IDataSeeder
{
    /// <summary>
    /// Controls execution order across modules. Lower runs first. Use it to respect real
    /// dependencies only — students cannot be enrolled before the classes they belong to exist.
    /// </summary>
    int Order { get; }

    /// <summary>Human-readable name, used in the log line for this step.</summary>
    string Name { get; }

    Task SeedAsync(CancellationToken cancellationToken = default);
}
