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
    public void An_empty_submission_is_rejected()
    {
        Should.Throw<DomainException>(() => Create(text: "  "));
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

        Should.Throw<DomainException>(() => submission.Replace("друго", Today.AddDays(1), Today));
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
    public void A_mark_already_in_the_gradebook_cannot_be_changed()
    {
        var submission = Create();
        submission.Review(5m, null, TeacherId, assignmentIsGraded: true);
        submission.AttachGrade(Guid.CreateVersion7());

        Should.Throw<DomainException>(() => submission.Review(6m, null, TeacherId, assignmentIsGraded: true));
    }

    [Fact]
    public void Feedback_can_still_be_changed_after_the_mark_reached_the_gradebook()
    {
        var submission = Create();
        submission.Review(5m, "първо", TeacherId, assignmentIsGraded: true);
        submission.AttachGrade(Guid.CreateVersion7());
        submission.Review(5m, "второ", TeacherId, assignmentIsGraded: true);

        submission.Feedback.ShouldBe("второ");
        submission.Mark.ShouldBe(5m);
    }

    private static Submission Create(string? text = "Отговор", DateOnly? dueOn = null) =>
        Submission.Create(AssignmentId, StudentId, text, dueOn ?? Today.AddDays(5), Today);
}
