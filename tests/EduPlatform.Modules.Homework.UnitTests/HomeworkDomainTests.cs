using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Homework.Domain;
using Shouldly;

namespace EduPlatform.Modules.Homework.UnitTests;

public sealed class AssignmentTests
{
    private static readonly Guid TeachingAssignmentId = Guid.CreateVersion7();
    private static readonly Guid TeacherId = Guid.CreateVersion7();
    private static readonly DateOnly Today = new(2026, 11, 7);

    [Fact]
    public void A_deadline_in_the_past_is_rejected()
    {
        Should.Throw<DomainException>(() => Create(dueOn: Today.AddDays(-1)));
    }

    [Fact]
    public void A_deadline_of_today_is_accepted()
    {
        Create(dueOn: Today).DueOn.ShouldBe(Today);
    }

    [Fact]
    public void A_blank_title_is_rejected()
    {
        Should.Throw<DomainException>(() => Create(title: "   "));
    }

    [Fact]
    public void A_title_longer_than_the_limit_is_rejected()
    {
        Should.Throw<DomainException>(() => Create(title: new string('x', Assignment.MaxTitleLength + 1)));
    }

    [Fact]
    public void The_title_is_trimmed()
    {
        Create(title: "  Задачи 1-10  ").Title.ShouldBe("Задачи 1-10");
    }

    [Fact]
    public void An_empty_description_becomes_null()
    {
        Create(description: "   ").Description.ShouldBeNull();
    }

    [Fact]
    public void A_submission_after_the_deadline_is_not_accepted()
    {
        var assignment = Create(dueOn: Today.AddDays(3));

        assignment.AcceptsSubmissionOn(Today.AddDays(3)).ShouldBeTrue();
        assignment.AcceptsSubmissionOn(Today.AddDays(4)).ShouldBeFalse();
    }

    [Fact]
    public void A_homework_backed_by_a_test_cannot_also_be_graded_by_hand()
    {
        Should.Throw<DomainException>(() => Create(isGraded: true).AttachExam(Guid.CreateVersion7()));
    }

    [Fact]
    public void A_homework_without_its_own_mark_can_point_at_a_test()
    {
        var assignment = Create(isGraded: false);
        var examId = Guid.CreateVersion7();

        assignment.AttachExam(examId);

        assignment.IsExam.ShouldBeTrue();
        assignment.ExamId.ShouldBe(examId);
    }

    [Fact]
    public void An_empty_test_reference_is_rejected()
    {
        Should.Throw<DomainException>(() => Create(isGraded: false).AttachExam(Guid.Empty));
    }

    internal static Assignment Create(
        string? title = "Задачи 1-10",
        string? description = null,
        DateOnly? dueOn = null,
        bool isGraded = true) =>
        Assignment.Create(
            TeachingAssignmentId, title, description, dueOn ?? Today.AddDays(7), isGraded, TeacherId, Today);
}

public sealed class SubmissionTests
{
    private static readonly Guid AssignmentId = Guid.CreateVersion7();
    private static readonly Guid StudentId = Guid.CreateVersion7();
    private static readonly Guid TeacherId = Guid.CreateVersion7();
    private static readonly DateOnly Today = new(2026, 11, 7);

    [Fact]
    public void An_empty_submission_with_no_file_is_rejected()
    {
        Should.Throw<DomainException>(() => Create(text: "  "));
    }

    [Fact]
    public void A_submission_with_only_a_file_is_accepted()
    {
        Create(text: "  ", hasAttachment: true).Text.ShouldBeEmpty();
    }

    [Fact]
    public void An_attachment_records_its_name_and_size()
    {
        var submission = Create();
        submission.Attach("2026/11/abc.pdf", "resheniya.pdf", "application/pdf", 1024);

        submission.HasAttachment.ShouldBeTrue();
        submission.FileName.ShouldBe("resheniya.pdf");
        submission.SizeBytes.ShouldBe(1024);
    }

    [Fact]
    public void A_file_type_outside_the_allowed_list_is_rejected()
    {
        Should.Throw<DomainException>(() => Create().Attach("k", "virus.exe", "application/octet-stream", 10));
    }

    [Fact]
    public void A_file_over_the_size_limit_is_rejected()
    {
        Should.Throw<DomainException>(
            () => Create().Attach("k", "big.zip", "application/zip", FileAttachmentRules.MaxFileSizeBytes + 1));
    }

    [Fact]
    public void A_reviewed_submission_cannot_get_a_new_file()
    {
        var submission = Create();
        submission.Review(5m, null, TeacherId, assignmentIsGraded: true);

        Should.Throw<DomainException>(() => submission.Attach("k", "late.pdf", "application/pdf", 10));
    }

    [Fact]
    public void Detaching_returns_the_key_it_replaced()
    {
        var submission = Create();
        submission.Attach("2026/11/old.pdf", "old.pdf", "application/pdf", 10);

        submission.DetachFile().ShouldBe("2026/11/old.pdf");
        submission.HasAttachment.ShouldBeFalse();
    }

    [Fact]
    public void A_submission_on_the_deadline_is_not_late()
    {
        Create(dueOn: Today).IsLate.ShouldBeFalse();
    }

    [Fact]
    public void A_submission_after_the_deadline_is_late()
    {
        Create(dueOn: Today.AddDays(-1)).IsLate.ShouldBeTrue();
    }

    [Fact]
    public void A_reviewed_submission_can_no_longer_be_replaced()
    {
        var submission = Create();
        submission.Review(5m, "ok", TeacherId, assignmentIsGraded: true);

        Should.Throw<DomainException>(() => submission.Replace("друго", hasAttachment: false, Today.AddDays(1), Today));
    }

    [Theory]
    [InlineData(1.99)]
    [InlineData(6.01)]
    public void A_mark_outside_the_scale_is_rejected(decimal mark)
    {
        Should.Throw<DomainException>(() => Create().Review(mark, null, TeacherId, assignmentIsGraded: true));
    }

    [Fact]
    public void A_mark_on_homework_that_is_not_graded_is_rejected()
    {
        Should.Throw<DomainException>(() => Create().Review(5m, null, TeacherId, assignmentIsGraded: false));
    }

    [Fact]
    public void Feedback_without_a_mark_is_allowed_on_homework_that_is_not_graded()
    {
        var submission = Create();
        submission.Review(null, "Прочетох я.", TeacherId, assignmentIsGraded: false);

        submission.IsReviewed.ShouldBeTrue();
        submission.Mark.ShouldBeNull();
        submission.Feedback.ShouldBe("Прочетох я.");
    }

    [Fact]
    public void A_mark_is_rounded_away_from_zero()
    {
        var submission = Create();
        submission.Review(5.455m, null, TeacherId, assignmentIsGraded: true);

        submission.Mark.ShouldBe(5.46m);
    }

    [Fact]
    public void A_mark_already_in_the_gradebook_can_be_corrected()
    {
        var submission = Create();
        submission.Review(5m, null, TeacherId, assignmentIsGraded: true);
        submission.AttachGrade(Guid.CreateVersion7());

        submission.Review(6m, null, TeacherId, assignmentIsGraded: true);

        submission.Mark.ShouldBe(6m);
    }

    [Fact]
    public void Feedback_can_be_changed_without_touching_the_mark()
    {
        var submission = Create();
        submission.Review(5m, "първо", TeacherId, assignmentIsGraded: true);
        submission.Review(null, "второ", TeacherId, assignmentIsGraded: true);

        submission.Feedback.ShouldBe("второ");
        submission.Mark.ShouldBe(5m);
    }

    [Fact]
    public void Detaching_the_grade_leaves_the_mark_in_place()
    {
        var submission = Create();
        submission.Review(5m, null, TeacherId, assignmentIsGraded: true);
        submission.AttachGrade(Guid.CreateVersion7());

        submission.DetachGrade();

        submission.GradeId.ShouldBeNull();
        submission.Mark.ShouldBe(5m);
    }

    private static Submission Create(
        string? text = "Отговор",
        DateOnly? dueOn = null,
        bool hasAttachment = false) =>
        Submission.Create(AssignmentId, StudentId, text, hasAttachment, dueOn ?? Today.AddDays(5), Today);
}
