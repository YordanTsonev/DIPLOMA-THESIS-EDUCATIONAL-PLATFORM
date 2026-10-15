using EduPlatform.BuildingBlocks.Infrastructure.Persistence;
using EduPlatform.Modules.SchoolStructure.Domain;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Modules.SchoolStructure.Infrastructure.Persistence;

public sealed class SchoolStructureDbContext(DbContextOptions<SchoolStructureDbContext> options)
    : ModuleDbContext(options)
{
    public const string SchemaName = "school";

    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();

    public DbSet<Subject> Subjects => Set<Subject>();

    public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();

    public DbSet<ClassEnrollment> ClassEnrollments => Set<ClassEnrollment>();

    public DbSet<TeachingAssignment> TeachingAssignments => Set<TeachingAssignment>();

    public DbSet<ScheduleSlot> ScheduleSlots => Set<ScheduleSlot>();

    protected override string Schema => SchemaName;
}
