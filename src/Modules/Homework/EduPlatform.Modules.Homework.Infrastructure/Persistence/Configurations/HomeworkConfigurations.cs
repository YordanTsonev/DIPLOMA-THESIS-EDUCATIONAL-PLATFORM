using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Homework.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduPlatform.Modules.Homework.Infrastructure.Persistence.Configurations;

internal sealed class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("assignments");
        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.TeachingAssignmentId).IsRequired();
        builder.Property(assignment => assignment.Title).HasMaxLength(Assignment.MaxTitleLength).IsRequired();
        builder.Property(assignment => assignment.Description).HasMaxLength(Assignment.MaxDescriptionLength);
        builder.Property(assignment => assignment.DueOn).IsRequired();
        builder.Property(assignment => assignment.IsGraded).IsRequired();
        builder.Property(assignment => assignment.CreatedByUserId).IsRequired();
        builder.Property(assignment => assignment.CreatedAt).IsRequired();

        builder.Property(assignment => assignment.StorageKey).HasMaxLength(400);
        builder.Property(assignment => assignment.FileName).HasMaxLength(FileAttachmentRules.MaxFileNameLength);
        builder.Property(assignment => assignment.ContentType).HasMaxLength(160);

        builder.Ignore(assignment => assignment.DomainEvents);
        builder.Ignore(assignment => assignment.HasAttachment);
        builder.HasIndex(assignment => assignment.TeachingAssignmentId);
    }
}

internal sealed class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("submissions");
        builder.HasKey(submission => submission.Id);

        builder.Property(submission => submission.AssignmentId).IsRequired();
        builder.Property(submission => submission.StudentUserId).IsRequired();
        builder.Property(submission => submission.Text).HasMaxLength(Submission.MaxTextLength).IsRequired();
        builder.Property(submission => submission.SubmittedAt).IsRequired();
        builder.Property(submission => submission.IsLate).IsRequired();
        builder.Property(submission => submission.Mark).HasPrecision(4, 2);
        builder.Property(submission => submission.Feedback).HasMaxLength(Submission.MaxFeedbackLength);

        builder.Property(submission => submission.StorageKey).HasMaxLength(400);
        builder.Property(submission => submission.FileName).HasMaxLength(FileAttachmentRules.MaxFileNameLength);
        builder.Property(submission => submission.ContentType).HasMaxLength(160);

        builder.Ignore(submission => submission.DomainEvents);
        builder.Ignore(submission => submission.IsReviewed);
        builder.Ignore(submission => submission.HasAttachment);

        builder.HasIndex(submission => new { submission.AssignmentId, submission.StudentUserId }).IsUnique();

        builder.HasOne<Assignment>()
            .WithMany()
            .HasForeignKey(submission => submission.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
