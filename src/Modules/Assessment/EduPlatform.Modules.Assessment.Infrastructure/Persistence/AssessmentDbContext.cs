using EduPlatform.BuildingBlocks.Infrastructure.Persistence;
using EduPlatform.Modules.Assessment.Domain;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Modules.Assessment.Infrastructure.Persistence;

public sealed class AssessmentDbContext(DbContextOptions<AssessmentDbContext> options) : ModuleDbContext(options)
{
    public const string SchemaName = "assessment";

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<Exam> Exams => Set<Exam>();

    public DbSet<Attempt> Attempts => Set<Attempt>();

    protected override string Schema => SchemaName;
}
