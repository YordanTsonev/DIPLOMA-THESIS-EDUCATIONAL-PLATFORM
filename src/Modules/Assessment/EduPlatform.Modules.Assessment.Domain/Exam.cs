using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.Assessment.Domain;

public sealed class ExamItem : Entity
{
    private ExamItem(Guid id, Guid questionId, int points, int position)
        : base(id)
    {
        QuestionId = questionId;
        Points = points;
        Position = position;
    }

    private ExamItem() { }

    public Guid QuestionId { get; private set; }

    public int Points { get; private set; }

    public int Position { get; private set; }

    internal static ExamItem Create(Guid questionId, int points, int position)
    {
        if (questionId == Guid.Empty)
        {
            throw new DomainException("A question is required.");
        }

        if (points < Exam.MinPoints || points > Exam.MaxPoints)
        {
            throw new DomainException($"Points must be between {Exam.MinPoints} and {Exam.MaxPoints}.");
        }

        return new ExamItem(Guid.CreateVersion7(), questionId, points, position);
    }
}

public sealed class Exam : AggregateRoot
{
    public const int MaxTitleLength = 160;
    public const int MaxDescriptionLength = 2000;
    public const int MinPoints = 1;
    public const int MaxPoints = 100;
    public const int MinDurationMinutes = 5;
    public const int MaxDurationMinutes = 240;

    private readonly List<ExamItem> _items = [];

    private Exam(
        Guid id,
        Guid teachingAssignmentId,
        string title,
        string? description,
        int durationMinutes,
        Guid createdByUserId)
        : base(id)
    {
        TeachingAssignmentId = teachingAssignmentId;
        Title = title;
        Description = description;
        DurationMinutes = durationMinutes;
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private Exam() { }

    public Guid TeachingAssignmentId { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public int DurationMinutes { get; private set; }

    public DateTimeOffset? OpensAt { get; private set; }

    public DateTimeOffset? ClosesAt { get; private set; }

    public bool IsPublished { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<ExamItem> Items => _items;

    public int MaxScore => _items.Sum(item => item.Points);

    public static Exam Create(
        Guid teachingAssignmentId,
        string? title,
        string? description,
        int durationMinutes,
        Guid createdByUserId)
    {
        if (teachingAssignmentId == Guid.Empty)
        {
            throw new DomainException("A test must belong to a taught subject.");
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new DomainException("The author of the test is required.");
        }

        var trimmed = title?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new DomainException("A test title is required.");
        }

        if (trimmed.Length > MaxTitleLength)
        {
            throw new DomainException($"Title must not exceed {MaxTitleLength} characters.");
        }

        if (durationMinutes < MinDurationMinutes || durationMinutes > MaxDurationMinutes)
        {
            throw new DomainException(
                $"Duration must be between {MinDurationMinutes} and {MaxDurationMinutes} minutes.");
        }

        var note = description?.Trim();

        if (note is { Length: > MaxDescriptionLength })
        {
            throw new DomainException($"Description must not exceed {MaxDescriptionLength} characters.");
        }

        return new Exam(
            Guid.CreateVersion7(),
            teachingAssignmentId,
            trimmed,
            note is { Length: 0 } ? null : note,
            durationMinutes,
            createdByUserId);
    }

    public void AddItem(Guid questionId, int points)
    {
        RequireDraft();

        if (_items.Any(item => item.QuestionId == questionId))
        {
            throw new DomainException("This question is already in the test.");
        }

        _items.Add(ExamItem.Create(questionId, points, _items.Count));
    }

    public void RemoveItem(Guid questionId)
    {
        RequireDraft();

        var item = _items.SingleOrDefault(entry => entry.QuestionId == questionId)
            ?? throw new DomainException("This question is not in the test.");

        _items.Remove(item);
    }

    public void Publish(DateTimeOffset? opensAt, DateTimeOffset? closesAt)
    {
        if (_items.Count == 0)
        {
            throw new DomainException("A test needs at least one question before it can be published.");
        }

        if (opensAt is { } start && closesAt is { } end && end <= start)
        {
            throw new DomainException("A test cannot close before it opens.");
        }

        OpensAt = opensAt;
        ClosesAt = closesAt;
        IsPublished = true;
    }

    public void Withdraw()
    {
        IsPublished = false;
    }

    public bool AcceptsAttemptAt(DateTimeOffset moment) =>
        IsPublished
        && (OpensAt is not { } start || moment >= start)
        && (ClosesAt is not { } end || moment <= end);

    private void RequireDraft()
    {
        if (IsPublished)
        {
            throw new DomainException("A published test can no longer be changed.");
        }
    }
}
