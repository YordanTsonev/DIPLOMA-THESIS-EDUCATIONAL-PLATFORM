using EduPlatform.BuildingBlocks.Infrastructure.Persistence;
using EduPlatform.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// The Identity module's unit of work. Owns the <c>identity</c> schema and nothing else.
/// </summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : ModuleDbContext(options)
{
    public const string SchemaName = "identity";

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override string Schema => SchemaName;
}
