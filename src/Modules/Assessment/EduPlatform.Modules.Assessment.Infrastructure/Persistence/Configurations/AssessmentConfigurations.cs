using EduPlatform.Modules.Assessment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduPlatform.Modules.Assessment.Infrastructure.Persistence.Configurations;

internal sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("questions");
        builder.HasKey(question => question.Id);

        builder.Property(question => question.SubjectId).IsRequired();
        builder.Property(question => question.Kind).HasConversion<int>().IsRequired();
        builder.Property(question => question.Text).HasMaxLength(Question.MaxTextLength).IsRequired();
        builder.Property(question => question.Explanation).HasMaxLength(Question.MaxExplanationLength);
        builder.Property(question => question.AuthorUserId).IsRequired();
        builder.Property(question => question.CreatedAt).IsRequired();

        builder.Ignore(question => question.DomainEvents);
        builder.Ignore(question => question.IsOpen);

        builder.HasIndex(question => question.SubjectId);
        builder.HasIndex(question => question.AuthorUserId);

        builder.OwnsMany(question => question.Options, options =>
        {
            options.ToTable("question_options");
            options.WithOwner().HasForeignKey("question_id");
            options.HasKey(option => option.Id);

            options.Property(option => option.Text).HasMaxLength(Question.MaxOptionLength).IsRequired();
            options.Property(option => option.IsCorrect).IsRequired();
            options.Property(option => option.Position).IsRequired();
        });

        builder.Navigation(question => question.Options).AutoInclude();
    }
}

internal sealed class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("exams");
        builder.HasKey(exam => exam.Id);

        builder.Property(exam => exam.TeachingAssignmentId).IsRequired();
        builder.Property(exam => exam.Title).HasMaxLength(Exam.MaxTitleLength).IsRequired();
        builder.Property(exam => exam.Description).HasMaxLength(Exam.MaxDescriptionLength);
        builder.Property(exam => exam.DurationMinutes).IsRequired();
        builder.Property(exam => exam.IsPublished).IsRequired();
        builder.Property(exam => exam.CreatedByUserId).IsRequired();
        builder.Property(exam => exam.CreatedAt).IsRequired();

        builder.Ignore(exam => exam.DomainEvents);
        builder.Ignore(exam => exam.MaxScore);

        builder.HasIndex(exam => exam.TeachingAssignmentId);

        builder.OwnsMany(exam => exam.Items, items =>
        {
            items.ToTable("exam_items");
            items.WithOwner().HasForeignKey("exam_id");
            items.HasKey(item => item.Id);

            items.Property(item => item.QuestionId).IsRequired();
            items.Property(item => item.Points).IsRequired();
            items.Property(item => item.Position).IsRequired();

            items.HasIndex(item => item.QuestionId);
        });

        builder.Navigation(exam => exam.Items).AutoInclude();
    }
}

internal sealed class AttemptConfiguration : IEntityTypeConfiguration<Attempt>
{
    public void Configure(EntityTypeBuilder<Attempt> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("attempts");
        builder.HasKey(attempt => attempt.Id);

        builder.Property(attempt => attempt.ExamId).IsRequired();
        builder.Property(attempt => attempt.StudentUserId).IsRequired();
        builder.Property(attempt => attempt.StartedAt).IsRequired();
        builder.Property(attempt => attempt.Deadline).IsRequired();
        builder.Property(attempt => attempt.MaxScore).IsRequired();
        builder.Property(attempt => attempt.Mark).HasPrecision(4, 2);

        builder.Ignore(attempt => attempt.DomainEvents);
        builder.Ignore(attempt => attempt.IsSubmitted);
        builder.Ignore(attempt => attempt.IsReleased);
        builder.Ignore(attempt => attempt.IsFullyScored);

        builder.HasIndex(attempt => new { attempt.ExamId, attempt.StudentUserId }).IsUnique();
        builder.HasIndex(attempt => attempt.StudentUserId);

        builder.HasOne<Exam>()
            .WithMany()
            .HasForeignKey(attempt => attempt.ExamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsMany(attempt => attempt.Answers, answers =>
        {
            answers.ToTable("attempt_answers");
            answers.WithOwner().HasForeignKey("attempt_id");
            answers.HasKey(answer => answer.Id);

            answers.Property(answer => answer.QuestionId).IsRequired();
            answers.Property(answer => answer.Text).HasMaxLength(Attempt.MaxAnswerLength);
            answers.Property(answer => answer.Feedback).HasMaxLength(Attempt.MaxFeedbackLength);
            answers.Property(answer => answer.SelectedOptionIds).HasColumnType("uuid[]");
        });

        builder.Navigation(attempt => attempt.Answers).AutoInclude();
    }
}
