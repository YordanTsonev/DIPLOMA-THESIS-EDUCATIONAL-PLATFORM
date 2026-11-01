using EduPlatform.Modules.Gradebook.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduPlatform.Modules.Gradebook.Infrastructure.Persistence.Configurations;

internal sealed class GradeConfiguration : IEntityTypeConfiguration<Grade>
{
    public void Configure(EntityTypeBuilder<Grade> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("grades");
        builder.HasKey(grade => grade.Id);

        builder.Property(grade => grade.StudentUserId).IsRequired();
        builder.Property(grade => grade.TeachingAssignmentId).IsRequired();
        builder.Property(grade => grade.Value).HasPrecision(3, 2).IsRequired();
        builder.Property(grade => grade.Kind).HasConversion<int>().IsRequired();
        builder.Property(grade => grade.Reason).HasConversion<int>().IsRequired();
        builder.Property(grade => grade.Comment).HasMaxLength(Grade.MaxCommentLength);
        builder.Property(grade => grade.TeacherUserId).IsRequired();
        builder.Property(grade => grade.GivenAt).IsRequired();

        builder.Ignore(grade => grade.DomainEvents);

        builder.HasIndex(grade => new { grade.StudentUserId, grade.TeachingAssignmentId });
        builder.HasIndex(grade => grade.TeachingAssignmentId);
    }
}

internal sealed class AbsenceConfiguration : IEntityTypeConfiguration<Absence>
{
    public void Configure(EntityTypeBuilder<Absence> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("absences");
        builder.HasKey(absence => absence.Id);

        builder.Property(absence => absence.StudentUserId).IsRequired();
        builder.Property(absence => absence.TeachingAssignmentId).IsRequired();
        builder.Property(absence => absence.Date).IsRequired();
        builder.Property(absence => absence.Period).IsRequired();
        builder.Property(absence => absence.Kind).HasConversion<int>().IsRequired();
        builder.Property(absence => absence.Note).HasMaxLength(Absence.MaxNoteLength);
        builder.Property(absence => absence.TeacherUserId).IsRequired();
        builder.Property(absence => absence.RecordedAt).IsRequired();

        builder.Ignore(absence => absence.Weight);
        builder.Ignore(absence => absence.DomainEvents);

        builder.HasIndex(absence => new
        {
            absence.StudentUserId,
            absence.TeachingAssignmentId,
            absence.Date,
            absence.Period,
        }).IsUnique();
    }
}
