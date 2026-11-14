using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.Homework.Domain;

public sealed class Submission : AggregateRoot
{
    public const int MaxTextLength = 8000;
    public const int MaxFeedbackLength = 1000;
    public const decimal MinMark = 2.00m;
    public const decimal MaxMark = 6.00m;

    private Submission(
        Guid id,
        Guid assignmentId,
        Guid studentUserId,
        string text,
        bool isLate)
        : base(id)
    {
        AssignmentId = assignmentId;
        StudentUserId = studentUserId;
        Text = text;
        IsLate = isLate;
        SubmittedAt = DateTimeOffset.UtcNow;
    }

    private Submission() { }

    public Guid AssignmentId { get; private set; }

    public Guid StudentUserId { get; private set; }

    public string Text { get; private set; } = null!;

    public DateTimeOffset SubmittedAt { get; private set; }

    public bool IsLate { get; private set; }

    public decimal? Mark { get; private set; }

    public string? Feedback { get; private set; }

    public Guid? ReviewedByUserId { get; private set; }

    public DateTimeOffset? ReviewedAt { get; private set; }

    public Guid? GradeId { get; private set; }

    public bool IsReviewed => ReviewedAt is not null;

    public string? StorageKey { get; private set; }

    public string? FileName { get; private set; }

    public string? ContentType { get; private set; }

    public long? SizeBytes { get; private set; }

    public bool HasAttachment => StorageKey is not null;

    public void Attach(string storageKey, string? fileName, string? contentType, long sizeBytes)
    {
        if (IsReviewed)
        {
            throw new DomainException("A reviewed submission can no longer be changed.");
        }

        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new DomainException("A storage key is required.");
        }

        StorageKey = storageKey;
        FileName = FileAttachmentRules.Validate(fileName, sizeBytes);
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
        SizeBytes = sizeBytes;
    }

    public string? DetachFile()
    {
        var previous = StorageKey;

        StorageKey = null;
        FileName = null;
        ContentType = null;
        SizeBytes = null;

        return previous;
    }

    public static Submission Create(
        Guid assignmentId,
        Guid studentUserId,
        string? text,
        bool hasAttachment,
        DateOnly dueOn,
        DateOnly today)
    {
        if (assignmentId == Guid.Empty)
        {
            throw new DomainException("A submission must belong to a homework assignment.");
        }

        if (studentUserId == Guid.Empty)
        {
            throw new DomainException("The author of the submission is required.");
        }

        return new Submission(
            Guid.CreateVersion7(), assignmentId, studentUserId, Validate(text, hasAttachment), today > dueOn);
    }

    public void Replace(string? text, bool hasAttachment, DateOnly dueOn, DateOnly today)
    {
        if (IsReviewed)
        {
            throw new DomainException("A reviewed submission can no longer be changed.");
        }

        Text = Validate(text, hasAttachment);
        IsLate = today > dueOn;
        SubmittedAt = DateTimeOffset.UtcNow;
    }

    public void Review(decimal? mark, string? feedback, Guid reviewerId, bool assignmentIsGraded)
    {
        if (reviewerId == Guid.Empty)
        {
            throw new DomainException("The reviewer is required.");
        }

        if (mark is not null && !assignmentIsGraded)
        {
            throw new DomainException("This homework was not set up to be graded.");
        }

        if (mark is { } value)
        {
            var rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);

            if (rounded < MinMark || rounded > MaxMark)
            {
                throw new DomainException($"A mark must be between {MinMark:0.00} and {MaxMark:0.00}.");
            }

            Mark = rounded;
        }

        var trimmed = feedback?.Trim();

        if (trimmed is { Length: > MaxFeedbackLength })
        {
            throw new DomainException($"Feedback must not exceed {MaxFeedbackLength} characters.");
        }

        Feedback = trimmed is { Length: 0 } ? null : trimmed;
        ReviewedByUserId = reviewerId;
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    public void DetachGrade() => GradeId = null;

    public void AttachGrade(Guid gradeId)
    {
        if (gradeId == Guid.Empty)
        {
            throw new DomainException("The grade is required.");
        }

        GradeId = gradeId;
    }

    private static string Validate(string? text, bool hasAttachment)
    {
        var trimmed = text?.Trim() ?? string.Empty;

        if (trimmed.Length == 0 && !hasAttachment)
        {
            throw new DomainException("A submission needs text, a file, or both.");
        }

        if (trimmed.Length > MaxTextLength)
        {
            throw new DomainException($"A submission must not exceed {MaxTextLength} characters.");
        }

        return trimmed;
    }
}
