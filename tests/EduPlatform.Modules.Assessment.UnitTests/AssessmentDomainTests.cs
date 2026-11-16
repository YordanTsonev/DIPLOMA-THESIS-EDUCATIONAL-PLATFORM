using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Assessment.Domain;
using Shouldly;

namespace EduPlatform.Modules.Assessment.UnitTests;

public sealed class QuestionTests
{
    private static readonly Guid SubjectId = Guid.CreateVersion7();
    private static readonly Guid AuthorId = Guid.CreateVersion7();

    [Fact]
    public void A_single_choice_question_needs_exactly_one_correct_option()
    {
        Should.Throw<DomainException>(() => Create(
            QuestionKind.SingleChoice, [("a", true), ("b", true)]));
    }

    [Fact]
    public void A_closed_question_needs_at_least_one_correct_option()
    {
        Should.Throw<DomainException>(() => Create(
            QuestionKind.MultipleChoice, [("a", false), ("b", false)]));
    }

    [Fact]
    public void A_closed_question_needs_at_least_two_options()
    {
        Should.Throw<DomainException>(() => Create(QuestionKind.SingleChoice, [("a", true)]));
    }

    [Fact]
    public void An_open_question_must_not_carry_options()
    {
        Should.Throw<DomainException>(() => Create(QuestionKind.OpenText, [("a", true), ("b", false)]));
    }

    [Fact]
    public void An_open_question_with_no_options_is_accepted()
    {
        Create(QuestionKind.OpenText, []).IsOpen.ShouldBeTrue();
    }

    [Fact]
    public void A_blank_question_text_is_rejected()
    {
        Should.Throw<DomainException>(() => Question.Create(
            SubjectId, QuestionKind.OpenText, "   ", null, [], AuthorId));
    }

    [Fact]
    public void A_multiple_choice_answer_must_match_every_correct_option()
    {
        var question = Create(QuestionKind.MultipleChoice, [("a", true), ("b", false), ("c", true)]);
        var correct = question.Options.Where(option => option.IsCorrect).Select(option => option.Id).ToArray();
        var partial = correct.Take(1).ToArray();

        question.IsAnsweredCorrectlyBy(correct).ShouldBeTrue();
        question.IsAnsweredCorrectlyBy(partial).ShouldBeFalse();
        question.IsAnsweredCorrectlyBy([]).ShouldBeFalse();
    }

    [Fact]
    public void An_open_question_is_never_scored_automatically()
    {
        Create(QuestionKind.OpenText, []).IsAnsweredCorrectlyBy([]).ShouldBeFalse();
    }

    internal static Question Create(QuestionKind kind, (string Text, bool IsCorrect)[] options) =>
        Question.Create(SubjectId, kind, "Въпрос?", null, options, AuthorId);
}

public sealed class ExamTests
{
    private static readonly Guid TeachingAssignmentId = Guid.CreateVersion7();
    private static readonly Guid TeacherId = Guid.CreateVersion7();

    [Fact]
    public void A_duration_outside_the_allowed_range_is_rejected()
    {
        Should.Throw<DomainException>(() => Create(durationMinutes: 4));
        Should.Throw<DomainException>(() => Create(durationMinutes: 241));
    }

    [Fact]
    public void An_empty_test_cannot_be_published()
    {
        Should.Throw<DomainException>(() => Create().Publish(null, null));
    }

    [Fact]
    public void A_published_test_can_no_longer_be_changed()
    {
        var exam = Create();
        exam.AddItem(Guid.CreateVersion7(), 3);
        exam.Publish(null, null);

        Should.Throw<DomainException>(() => exam.AddItem(Guid.CreateVersion7(), 2));
    }

    [Fact]
    public void The_same_question_cannot_be_added_twice()
    {
        var exam = Create();
        var questionId = Guid.CreateVersion7();
        exam.AddItem(questionId, 3);

        Should.Throw<DomainException>(() => exam.AddItem(questionId, 2));
    }

    [Fact]
    public void The_maximum_score_is_the_sum_of_the_points()
    {
        var exam = Create();
        exam.AddItem(Guid.CreateVersion7(), 3);
        exam.AddItem(Guid.CreateVersion7(), 5);

        exam.MaxScore.ShouldBe(8);
    }

    [Fact]
    public void A_test_cannot_close_before_it_opens()
    {
        var exam = Create();
        exam.AddItem(Guid.CreateVersion7(), 1);
        var now = DateTimeOffset.UtcNow;

        Should.Throw<DomainException>(() => exam.Publish(now.AddHours(2), now.AddHours(1)));
    }

    [Fact]
    public void A_test_outside_its_window_accepts_no_attempt()
    {
        var exam = Create();
        exam.AddItem(Guid.CreateVersion7(), 1);
        var now = DateTimeOffset.UtcNow;
        exam.Publish(now.AddHours(1), now.AddHours(2));

        exam.AcceptsAttemptAt(now).ShouldBeFalse();
        exam.AcceptsAttemptAt(now.AddMinutes(90)).ShouldBeTrue();
        exam.AcceptsAttemptAt(now.AddHours(3)).ShouldBeFalse();
    }

    internal static Exam Create(int durationMinutes = 30) =>
        Exam.Create(TeachingAssignmentId, "Контролно", null, durationMinutes, TeacherId);
}

public sealed class AttemptTests
{
    private static readonly Guid StudentId = Guid.CreateVersion7();

    [Fact]
    public void The_deadline_is_the_start_plus_the_duration()
    {
        var now = DateTimeOffset.UtcNow;
        var attempt = Start(now, durationMinutes: 30);

        (attempt.Deadline - now).TotalMinutes.ShouldBe(30, 0.01);
    }

    [Fact]
    public void The_deadline_never_passes_the_closing_time()
    {
        var now = DateTimeOffset.UtcNow;
        var exam = ExamTests.Create(durationMinutes: 60);
        exam.AddItem(Guid.CreateVersion7(), 2);
        exam.Publish(null, now.AddMinutes(10));

        Attempt.Start(exam, StudentId, now).Deadline.ShouldBe(now.AddMinutes(10));
    }

    [Fact]
    public void An_answer_after_the_deadline_is_rejected()
    {
        var now = DateTimeOffset.UtcNow;
        var attempt = Start(now, durationMinutes: 5);
        var questionId = attempt.Answers[0].QuestionId;

        Should.Throw<DomainException>(
            () => attempt.Answer(questionId, "късно", [], now.AddMinutes(6)));
    }

    [Fact]
    public void A_submitted_attempt_cannot_be_answered_again()
    {
        var now = DateTimeOffset.UtcNow;
        var attempt = Start(now, durationMinutes: 30);
        var questionId = attempt.Answers[0].QuestionId;

        attempt.Submit(Outcome(questionId, correct: true, points: 4), now);

        Should.Throw<DomainException>(() => attempt.Answer(questionId, "пак", [], now));
    }

    [Fact]
    public void A_correct_closed_answer_is_scored_automatically()
    {
        var now = DateTimeOffset.UtcNow;
        var attempt = Start(now, durationMinutes: 30);
        var questionId = attempt.Answers[0].QuestionId;

        attempt.Submit(Outcome(questionId, correct: true, points: 4), now);

        attempt.Score.ShouldBe(4);
        attempt.IsFullyScored.ShouldBeTrue();
    }

    [Fact]
    public void A_wrong_closed_answer_scores_nothing()
    {
        var now = DateTimeOffset.UtcNow;
        var attempt = Start(now, durationMinutes: 30);
        var questionId = attempt.Answers[0].QuestionId;

        attempt.Submit(Outcome(questionId, correct: false, points: 4), now);

        attempt.Score.ShouldBe(0);
    }

    [Fact]
    public void An_open_answer_waits_for_the_teacher()
    {
        var now = DateTimeOffset.UtcNow;
        var attempt = Start(now, durationMinutes: 30);
        var questionId = attempt.Answers[0].QuestionId;

        attempt.Submit(
            new Dictionary<Guid, (bool, bool, int)> { [questionId] = (true, false, 4) }, now);

        attempt.IsFullyScored.ShouldBeFalse();
        Should.Throw<DomainException>(() => attempt.Release(5m));

        attempt.Review(questionId, 3, "добре");

        attempt.IsFullyScored.ShouldBeTrue();
        attempt.Score.ShouldBe(3);
    }

    [Fact]
    public void A_late_submission_is_recorded_at_the_deadline()
    {
        var now = DateTimeOffset.UtcNow;
        var attempt = Start(now, durationMinutes: 5);
        var questionId = attempt.Answers[0].QuestionId;

        attempt.Submit(Outcome(questionId, correct: true, points: 4), now.AddMinutes(9));

        attempt.SubmittedAt.ShouldBe(attempt.Deadline);
    }

    [Fact]
    public void An_expired_attempt_is_scored_from_the_answers_saved_in_time()
    {
        var now = DateTimeOffset.UtcNow;
        var attempt = Start(now, durationMinutes: 5);
        var questionId = attempt.Answers[0].QuestionId;

        attempt.Answer(questionId, "решението", [], now.AddMinutes(4));
        attempt.Submit(Outcome(questionId, correct: true, points: 4), now.AddMinutes(30));

        attempt.Answers[0].Text.ShouldBe("решението");
        attempt.Score.ShouldBe(4);
        attempt.SubmittedAt.ShouldBe(attempt.Deadline);
    }

    [Theory]
    [InlineData(10, 10, 6.00)]
    [InlineData(9, 10, 6.00)]
    [InlineData(8, 10, 5.00)]
    [InlineData(7, 10, 4.00)]
    [InlineData(5, 10, 3.00)]
    [InlineData(4, 10, 2.00)]
    public void The_suggested_mark_follows_the_share_of_the_points(int score, int max, decimal expected)
    {
        var now = DateTimeOffset.UtcNow;
        var exam = ExamTests.Create();
        exam.AddItem(Guid.CreateVersion7(), max);
        exam.Publish(null, null);

        var attempt = Attempt.Start(exam, StudentId, now);
        var questionId = attempt.Answers[0].QuestionId;

        attempt.Submit(new Dictionary<Guid, (bool, bool, int)> { [questionId] = (true, false, max) }, now);
        attempt.Review(questionId, score, null);

        attempt.SuggestedMark().ShouldBe(expected);
    }

    [Fact]
    public void A_result_cannot_be_released_before_every_answer_is_scored()
    {
        var now = DateTimeOffset.UtcNow;
        var attempt = Start(now, durationMinutes: 30);
        var questionId = attempt.Answers[0].QuestionId;

        attempt.Submit(new Dictionary<Guid, (bool, bool, int)> { [questionId] = (true, false, 4) }, now);

        Should.Throw<DomainException>(() => attempt.Release(5m));
    }

    [Fact]
    public void A_mark_outside_the_scale_is_rejected_on_release()
    {
        var now = DateTimeOffset.UtcNow;
        var attempt = Start(now, durationMinutes: 30);
        var questionId = attempt.Answers[0].QuestionId;
        attempt.Submit(Outcome(questionId, correct: true, points: 4), now);

        Should.Throw<DomainException>(() => attempt.Release(6.5m));
    }

    private static Dictionary<Guid, (bool IsOpen, bool IsCorrect, int Points)> Outcome(
        Guid questionId,
        bool correct,
        int points) =>
        new() { [questionId] = (false, correct, points) };

    private static Attempt Start(DateTimeOffset now, int durationMinutes)
    {
        var exam = ExamTests.Create(durationMinutes);
        exam.AddItem(Guid.CreateVersion7(), 4);
        exam.Publish(null, null);

        return Attempt.Start(exam, StudentId, now);
    }
}
