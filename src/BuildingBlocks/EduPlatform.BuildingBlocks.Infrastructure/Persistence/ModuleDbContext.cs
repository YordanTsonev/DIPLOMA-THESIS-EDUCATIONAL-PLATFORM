using Microsoft.EntityFrameworkCore;

namespace EduPlatform.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Base class for a module's <see cref="DbContext"/>.
/// </summary>
/// <remarks>
/// Each module keeps its tables in its own PostgreSQL schema and never maps another
/// module's tables. That is what makes the module boundary real at the database level
/// rather than only a naming convention.
/// </remarks>
public abstract class ModuleDbContext(DbContextOptions options) : DbContext(options)
{
    /// <summary>The PostgreSQL schema owned by this module, for example <c>identity</c>.</summary>
    protected abstract string Schema { get; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
