using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

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

        // Identifiers are UUIDv7 values produced by the domain, never by the database.
        //
        // This has to be stated explicitly. By default EF treats a Guid key as store-generated,
        // and then uses "the key already has a value" to decide that an entity reached through a
        // navigation property must be an existing row. A freshly created child — a refresh token
        // added to a loaded user, say — is therefore marked Modified instead of Added, and EF
        // emits an UPDATE that matches nothing and fails as a concurrency conflict.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var primaryKey = entityType.FindPrimaryKey();
            if (primaryKey is { Properties: [{ ClrType: var clrType } property] } && clrType == typeof(Guid))
            {
                property.ValueGenerated = ValueGenerated.Never;
            }
        }

        base.OnModelCreating(modelBuilder);
    }
}
