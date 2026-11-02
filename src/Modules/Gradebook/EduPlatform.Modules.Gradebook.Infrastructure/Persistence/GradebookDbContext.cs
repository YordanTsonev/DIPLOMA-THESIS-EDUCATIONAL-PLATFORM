using EduPlatform.BuildingBlocks.Infrastructure.Persistence;
using EduPlatform.Modules.Gradebook.Domain;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Modules.Gradebook.Infrastructure.Persistence;

public sealed class GradebookDbContext(DbContextOptions<GradebookDbContext> options) : ModuleDbContext(options)
{
    public const string SchemaName = "gradebook";

    public DbSet<Grade> Grades => Set<Grade>();

    public DbSet<Absence> Absences => Set<Absence>();

    protected override string Schema => SchemaName;
}
