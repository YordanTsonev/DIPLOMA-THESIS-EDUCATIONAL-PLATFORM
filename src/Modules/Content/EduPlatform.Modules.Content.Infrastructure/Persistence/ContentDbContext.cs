using EduPlatform.BuildingBlocks.Infrastructure.Persistence;
using EduPlatform.Modules.Content.Domain;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Modules.Content.Infrastructure.Persistence;

public sealed class ContentDbContext(DbContextOptions<ContentDbContext> options) : ModuleDbContext(options)
{
    public const string SchemaName = "content";

    public DbSet<Material> Materials => Set<Material>();

    public DbSet<MaterialFolder> MaterialFolders => Set<MaterialFolder>();

    protected override string Schema => SchemaName;
}
