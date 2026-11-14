using EduPlatform.BuildingBlocks.Infrastructure.Persistence;
using EduPlatform.Modules.Homework.Domain;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Modules.Homework.Infrastructure.Persistence;

public sealed class HomeworkDbContext(DbContextOptions<HomeworkDbContext> options) : ModuleDbContext(options)
{
    public const string SchemaName = "homework";

    public DbSet<Assignment> Assignments => Set<Assignment>();

    public DbSet<Submission> Submissions => Set<Submission>();

    protected override string Schema => SchemaName;
}
