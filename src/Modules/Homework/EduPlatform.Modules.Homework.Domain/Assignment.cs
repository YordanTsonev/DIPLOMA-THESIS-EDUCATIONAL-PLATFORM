using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.Homework.Domain;

public sealed class Assignment : AggregateRoot
{
    public const int MaxTitleLength = 160;
    public const int MaxDescriptionLength = 4000;

    private Assignment(
        Guid id,
        Guid teachingAssignmentId,
        string title,
        string? description,
        DateOnly dueOn,
        bool isGraded,
        Guid createdByUserId)
        : base(id)
    {
        TeachingAssignmentId = teachingAssignmentId;
        Title = title;
        Description = description;
        DueOn = dueOn;
        IsGraded = isGraded;
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private Assignment() { }

    public Guid TeachingAssignmentId { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public DateOnly DueOn { get; private set; }

    public bool IsGraded { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public string? StorageKey { get; private set; }

    public string? FileName { get; private set; }

    public string? ContentType { get; private set; }

    public long? SizeBytes { get; private set; }

    public bool HasAttachment => StorageKey is not null;

    public Guid? ExamId { get; private set; }

    public bool IsExam => ExamId is not null;

    public void AttachExam(Guid examId)
    {
        if (examId == Guid.Empty)
        {
            throw new DomainException("The test is required.");
        }

        if (IsGraded)
        {
            throw new DomainException("A homework backed by a test is graded by the test itself.");
        }

        ExamId = examId;
    }

    public void Attach(string storageKey, string? fileName, string? contentType, long sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new DomainException("A storage key is required.");
        }

        StorageKey = storageKey;
        FileName = FileAttachmentRules.Validate(fileName, sizeBytes);
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
        SizeBytes = sizeBytes;
    }

    public static Assignment Create(
        Guid teachingAssignmentId,
        string? title,
        string? description,
        DateOnly dueOn,
        bool isGraded,
        Guid createdByUserId,
        DateOnly today)
    {
        if (teachingAssignmentId == Guid.Empty)
        {
            throw new DomainException("A homework assignment must belong to a taught subject.");
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new DomainException("The author of the assignment is required.");
        }

        var trimmedTitle = title?.Trim() ?? string.Empty;

        if (trimmedTitle.Length == 0)
        {
            throw new DomainException("A homework title is required.");
        }

        if (trimmedTitle.Length > MaxTitleLength)
        {
            throw new DomainException($"Title must not exceed {MaxTitleLength} characters.");
        }

        if (dueOn < today)
        {
            throw new DomainException("The deadline cannot be in the past.");
        }

        return new Assignment(
            Guid.CreateVersion7(),
            teachingAssignmentId,
            trimmedTitle,
            Describe(description),
            dueOn,
            isGraded,
            createdByUserId);
    }

    public bool AcceptsSubmissionOn(DateOnly date) => date <= DueOn;

    private static string? Describe(string? description)
    {
        var trimmed = description?.Trim();

        if (trimmed is { Length: 0 })
        {
            return null;
        }

        if (trimmed is { Length: > MaxDescriptionLength })
        {
            throw new DomainException($"Description must not exceed {MaxDescriptionLength} characters.");
        }

        return trimmed;
    }
}
