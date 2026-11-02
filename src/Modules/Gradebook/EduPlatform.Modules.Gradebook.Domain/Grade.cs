using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.Gradebook.Domain;

public enum GradeKind
{
    Current = 1,
    Term = 2,
    Annual = 3,
}

public enum GradeReason
{
    Oral = 1,
    Written = 2,
    Test = 3,
    Homework = 4,
    Project = 5,
    Activity = 6,
}

public sealed class Grade : AggregateRoot
{
    public const decimal MinValue = 2.00m;
    public const decimal MaxValue = 6.00m;
    public const int MaxCommentLength = 500;

    private Grade(
        Guid id,
        Guid studentUserId,
        Guid teachingAssignmentId,
        decimal value,
        GradeKind kind,
        GradeReason reason,
        string? comment,
        Guid teacherUserId)
        : base(id)
    {
        StudentUserId = studentUserId;
        TeachingAssignmentId = teachingAssignmentId;
        Value = value;
        Kind = kind;
        Reason = reason;
        Comment = comment;
        TeacherUserId = teacherUserId;
        GivenAt = DateTimeOffset.UtcNow;
    }

    private Grade() { }

    public Guid StudentUserId { get; private set; }

    public Guid TeachingAssignmentId { get; private set; }

    public decimal Value { get; private set; }

    public GradeKind Kind { get; private set; }

    public GradeReason Reason { get; private set; }

    public string? Comment { get; private set; }

    public Guid TeacherUserId { get; private set; }

    public DateTimeOffset GivenAt { get; private set; }

    public static Grade Give(
        Guid studentUserId,
        Guid teachingAssignmentId,
        decimal value,
        GradeKind kind,
        GradeReason reason,
        Guid teacherUserId,
        string? comment = null)
    {
        if (studentUserId == Guid.Empty)
        {
            throw new DomainException("A student is required.");
        }

        if (teachingAssignmentId == Guid.Empty)
        {
            throw new DomainException("A teaching assignment is required.");
        }

        if (teacherUserId == Guid.Empty)
        {
            throw new DomainException("A teacher is required.");
        }

        if (!Enum.IsDefined(kind) || !Enum.IsDefined(reason))
        {
            throw new DomainException("Unknown grade kind or reason.");
        }

        var grade = new Grade(
            Guid.CreateVersion7(),
            studentUserId,
            teachingAssignmentId,
            Validate(value),
            kind,
            reason,
            NormaliseComment(comment),
            teacherUserId);

        grade.Raise(new GradePublished(grade.Id, studentUserId, teachingAssignmentId, grade.Value));
        return grade;
    }

    public void Correct(decimal value, string? comment)
    {
        Value = Validate(value);
        Comment = NormaliseComment(comment);
    }

    private static decimal Validate(decimal value)
    {
        var rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);

        return rounded < MinValue || rounded > MaxValue
            ? throw new DomainException($"A grade must be between {MinValue:0.00} and {MaxValue:0.00}.")
            : rounded;
    }

    private static string? NormaliseComment(string? comment)
    {
        var trimmed = comment?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length > MaxCommentLength
            ? throw new DomainException($"Comment must not exceed {MaxCommentLength} characters.")
            : trimmed;
    }
}
