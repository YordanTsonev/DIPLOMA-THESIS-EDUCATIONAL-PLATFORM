using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.Assessment.Domain;

public sealed class AttemptAnswer : Entity
{
    private readonly List<Guid> _selectedOptionIds = [];

    private AttemptAnswer(Guid id, Guid questionId)
        : base(id)
    {
        QuestionId = questionId;
    }

    private AttemptAnswer() { }

    public Guid QuestionId { get; private set; }

    public string? Text { get; private set; }

    public IReadOnlyList<Guid> SelectedOptionIds => _selectedOptionIds;

    public int? AwardedPoints { get; private set; }

    public string? Feedback { get; private set; }

    internal static AttemptAnswer Create(Guid questionId) =>
        new(Guid.CreateVersion7(), questionId);

    internal void Fill(string? text, IReadOnlyCollection<Guid> selectedOptionIds)
    {
        ArgumentNullException.ThrowIfNull(selectedOptionIds);

        var trimmed = text?.Trim();

        if (trimmed is { Length: > Attempt.MaxAnswerLength })
        {
            throw new DomainException($"An answer must not exceed {Attempt.MaxAnswerLength} characters.");
        }

        Text = trimmed is { Length: 0 } ? null : trimmed;

        _selectedOptionIds.Clear();
        _selectedOptionIds.AddRange(selectedOptionIds.Distinct());
    }

    internal void Award(int points, string? feedback)
    {
        if (points < 0)
        {
            throw new DomainException("Awarded points cannot be negative.");
        }

        var trimmed = feedback?.Trim();

        if (trimmed is { Length: > Attempt.MaxFeedbackLength })
        {
            throw new DomainException($"Feedback must not exceed {Attempt.MaxFeedbackLength} characters.");
        }

        AwardedPoints = points;
        Feedback = trimmed is { Length: 0 } ? null : trimmed;
    }
}

public sealed class Attempt : AggregateRoot
{
    public const int MaxAnswerLength = 8000;
    public const int MaxFeedbackLength = 1000;
    public const decimal MinMark = 2.00m;
    public const decimal MaxMark = 6.00m;

    private readonly List<AttemptAnswer> _answers = [];

    private Attempt(Guid id, Guid examId, Guid studentUserId, DateTimeOffset startedAt, DateTimeOffset deadline)
        : base(id)
    {
        ExamId = examId;
        StudentUserId = studentUserId;
        StartedAt = startedAt;
        Deadline = deadline;
    }

    private Attempt() { }

    public Guid ExamId { get; private set; }

    public Guid StudentUserId { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset Deadline { get; private set; }

    public DateTimeOffset? SubmittedAt { get; private set; }

    public int? Score { get; private set; }

    public int MaxScore { get; private set; }

    public decimal? Mark { get; private set; }

    public Guid? GradeId { get; private set; }

    public DateTimeOffset? ReleasedAt { get; private set; }

    public IReadOnlyList<AttemptAnswer> Answers => _answers;

    public bool IsSubmitted => SubmittedAt is not null;

    public bool IsReleased => ReleasedAt is not null;

    public static Attempt Start(Exam exam, Guid studentUserId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(exam);

        if (studentUserId == Guid.Empty)
        {
            throw new DomainException("The student is required.");
        }

        if (!exam.AcceptsAttemptAt(now))
        {
            throw new DomainException("This test is not open right now.");
        }

        var deadline = now.AddMinutes(exam.DurationMinutes);

        if (exam.ClosesAt is { } end && end < deadline)
        {
            deadline = end;
        }

        var attempt = new Attempt(Guid.CreateVersion7(), exam.Id, studentUserId, now, deadline)
        {
            MaxScore = exam.MaxScore,
        };

        foreach (var item in exam.Items)
        {
            attempt._answers.Add(AttemptAnswer.Create(item.QuestionId));
        }

        return attempt;
    }

    public void Answer(Guid questionId, string? text, IReadOnlyCollection<Guid> selectedOptionIds, DateTimeOffset now)
    {
        if (IsSubmitted)
        {
            throw new DomainException("This attempt has already been submitted.");
        }

        if (now > Deadline)
        {
            throw new DomainException("The time for this test has run out.");
        }

        var answer = _answers.SingleOrDefault(entry => entry.QuestionId == questionId)
            ?? throw new DomainException("This question is not part of the test.");

        answer.Fill(text, selectedOptionIds);
    }

    public void Submit(IReadOnlyDictionary<Guid, (bool IsOpen, bool IsCorrect, int Points)> outcome, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        if (IsSubmitted)
        {
            throw new DomainException("This attempt has already been submitted.");
        }

        foreach (var answer in _answers)
        {
            if (!outcome.TryGetValue(answer.QuestionId, out var result) || result.IsOpen)
            {
                continue;
            }

            answer.Award(result.IsCorrect ? result.Points : 0, null);
        }

        SubmittedAt = now > Deadline ? Deadline : now;
        Recalculate();
    }

    public void Review(Guid questionId, int points, string? feedback)
    {
        if (!IsSubmitted)
        {
            throw new DomainException("An attempt can only be reviewed after it is submitted.");
        }

        var answer = _answers.SingleOrDefault(entry => entry.QuestionId == questionId)
            ?? throw new DomainException("This question is not part of the test.");

        answer.Award(points, feedback);
        Recalculate();
    }

    public bool IsFullyScored => _answers.All(answer => answer.AwardedPoints is not null);

    public decimal SuggestedMark()
    {
        if (MaxScore == 0)
        {
            return MinMark;
        }

        var share = (decimal)(Score ?? 0) / MaxScore;

        return share switch
        {
            >= 0.90m => 6.00m,
            >= 0.78m => 5.00m,
            >= 0.63m => 4.00m,
            >= 0.50m => 3.00m,
            _ => 2.00m,
        };
    }

    public void Release(decimal mark)
    {
        if (!IsSubmitted)
        {
            throw new DomainException("An attempt can only be released after it is submitted.");
        }

        if (!IsFullyScored)
        {
            throw new DomainException("Every answer must be scored before the result is released.");
        }

        var rounded = Math.Round(mark, 2, MidpointRounding.AwayFromZero);

        if (rounded < MinMark || rounded > MaxMark)
        {
            throw new DomainException($"A mark must be between {MinMark:0.00} and {MaxMark:0.00}.");
        }

        Mark = rounded;
        ReleasedAt = DateTimeOffset.UtcNow;
    }

    public void AttachGrade(Guid gradeId)
    {
        if (gradeId == Guid.Empty)
        {
            throw new DomainException("The grade is required.");
        }

        GradeId = gradeId;
    }

    public void DetachGrade() => GradeId = null;

    private void Recalculate() => Score = _answers.Sum(answer => answer.AwardedPoints ?? 0);
}
