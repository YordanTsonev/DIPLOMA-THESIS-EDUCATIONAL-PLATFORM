using EduPlatform.Modules.SchoolStructure.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduPlatform.Modules.SchoolStructure.Infrastructure.Persistence.Configurations;

internal sealed class AcademicYearConfiguration : IEntityTypeConfiguration<AcademicYear>
{
    public void Configure(EntityTypeBuilder<AcademicYear> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("academic_years");
        builder.HasKey(year => year.Id);

        builder.Property(year => year.Name).HasMaxLength(AcademicYear.MaxNameLength).IsRequired();
        builder.Property(year => year.StartsOn).IsRequired();
        builder.Property(year => year.EndsOn).IsRequired();
        builder.Property(year => year.IsCurrent).IsRequired();

        builder.HasIndex(year => year.Name).IsUnique();
        builder.Ignore(year => year.DomainEvents);
    }
}

internal sealed class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("subjects");
        builder.HasKey(subject => subject.Id);

        builder.Property(subject => subject.Name).HasMaxLength(Subject.MaxNameLength).IsRequired();
        builder.Property(subject => subject.Code).HasMaxLength(Subject.MaxCodeLength).IsRequired();
        builder.Property(subject => subject.IsActive).IsRequired();

        builder.HasIndex(subject => subject.Code).IsUnique();
        builder.Ignore(subject => subject.DomainEvents);
    }
}

internal sealed class SchoolClassConfiguration : IEntityTypeConfiguration<SchoolClass>
{
    public void Configure(EntityTypeBuilder<SchoolClass> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("school_classes");
        builder.HasKey(schoolClass => schoolClass.Id);

        builder.Property(schoolClass => schoolClass.AcademicYearId).IsRequired();
        builder.Property(schoolClass => schoolClass.Grade).IsRequired();
        builder.Property(schoolClass => schoolClass.Section).HasMaxLength(SchoolClass.MaxSectionLength).IsRequired();
        builder.Property(schoolClass => schoolClass.HomeroomTeacherId);

        builder.Ignore(schoolClass => schoolClass.DisplayName);
        builder.Ignore(schoolClass => schoolClass.ActiveStudentCount);
        builder.Ignore(schoolClass => schoolClass.DomainEvents);

        builder.HasIndex(schoolClass => new
        {
            schoolClass.AcademicYearId,
            schoolClass.Grade,
            schoolClass.Section,
        }).IsUnique();

        builder.HasOne<AcademicYear>()
            .WithMany()
            .HasForeignKey(schoolClass => schoolClass.AcademicYearId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(schoolClass => schoolClass.Enrollments)
            .WithOne()
            .HasForeignKey(enrollment => enrollment.SchoolClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(schoolClass => schoolClass.Enrollments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ClassEnrollmentConfiguration : IEntityTypeConfiguration<ClassEnrollment>
{
    public void Configure(EntityTypeBuilder<ClassEnrollment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("class_enrollments");
        builder.HasKey(enrollment => enrollment.Id);

        builder.Property(enrollment => enrollment.StudentUserId).IsRequired();
        builder.Property(enrollment => enrollment.EnrolledAt).IsRequired();
        builder.Property(enrollment => enrollment.WithdrawnAt);

        builder.Ignore(enrollment => enrollment.IsActive);

        builder.HasIndex(enrollment => new { enrollment.SchoolClassId, enrollment.StudentUserId }).IsUnique();
        builder.HasIndex(enrollment => enrollment.StudentUserId);
    }
}

internal sealed class TeachingAssignmentConfiguration : IEntityTypeConfiguration<TeachingAssignment>
{
    public void Configure(EntityTypeBuilder<TeachingAssignment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("teaching_assignments");
        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.TeacherUserId).IsRequired();
        builder.Property(assignment => assignment.SubjectId).IsRequired();
        builder.Property(assignment => assignment.SchoolClassId).IsRequired();
        builder.Property(assignment => assignment.WeeklyHours).IsRequired();

        builder.Ignore(assignment => assignment.DomainEvents);

        builder.HasIndex(assignment => new
        {
            assignment.SubjectId,
            assignment.SchoolClassId,
        }).IsUnique();

        builder.HasIndex(assignment => assignment.TeacherUserId);

        builder.HasOne<Subject>()
            .WithMany()
            .HasForeignKey(assignment => assignment.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<SchoolClass>()
            .WithMany()
            .HasForeignKey(assignment => assignment.SchoolClassId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ScheduleSlotConfiguration : IEntityTypeConfiguration<ScheduleSlot>
{
    public void Configure(EntityTypeBuilder<ScheduleSlot> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("schedule_slots");
        builder.HasKey(slot => slot.Id);

        builder.Property(slot => slot.TeachingAssignmentId).IsRequired();
        builder.Property(slot => slot.DayOfWeek).HasConversion<int>().IsRequired();
        builder.Property(slot => slot.Period).IsRequired();
        builder.Property(slot => slot.StartsAt).IsRequired();
        builder.Property(slot => slot.EndsAt).IsRequired();
        builder.Property(slot => slot.Room).HasMaxLength(ScheduleSlot.MaxRoomLength);

        builder.Ignore(slot => slot.DomainEvents);

        builder.HasIndex(slot => new { slot.TeachingAssignmentId, slot.DayOfWeek, slot.Period }).IsUnique();

        builder.HasOne<TeachingAssignment>()
            .WithMany()
            .HasForeignKey(slot => slot.TeachingAssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("calendar_events");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Type).HasConversion<int>().IsRequired();
        builder.Property(item => item.Title).HasMaxLength(CalendarEvent.MaxTitleLength).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(CalendarEvent.MaxDescriptionLength);
        builder.Property(item => item.StartsOn).IsRequired();
        builder.Property(item => item.EndsOn).IsRequired();
        builder.Property(item => item.CreatedByUserId).IsRequired();
        builder.Property(item => item.CreatedAt).IsRequired();

        builder.Ignore(item => item.IsSchoolWide);
        builder.Ignore(item => item.DomainEvents);

        builder.HasIndex(item => new { item.StartsOn, item.EndsOn });
        builder.HasIndex(item => item.SchoolClassId);

        builder.HasOne<SchoolClass>()
            .WithMany()
            .HasForeignKey(item => item.SchoolClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Subject>()
            .WithMany()
            .HasForeignKey(item => item.SubjectId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
