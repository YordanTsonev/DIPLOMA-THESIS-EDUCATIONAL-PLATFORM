namespace EduPlatform.Modules.Homework.Contracts;

public sealed record AssignmentDto(
    Guid Id,
    Guid TeachingAssignmentId,
    string SubjectName,
    string SchoolClassName,
    string Title,
    string? Description,
    DateOnly DueOn,
    bool IsGraded,
    bool IsOverdue,
    int SubmissionCount,
    int ReviewedCount,
    int StudentCount);

public sealed record SubmissionDto(
    Guid Id,
    Guid AssignmentId,
    Guid StudentUserId,
    string StudentName,
    string Text,
    DateTimeOffset SubmittedAt,
    bool IsLate,
    decimal? Mark,
    string? Feedback,
    DateTimeOffset? ReviewedAt,
    Guid? GradeId);

public sealed record StudentAssignmentDto(
    Guid Id,
    Guid TeachingAssignmentId,
    string SubjectName,
    string Title,
    string? Description,
    DateOnly DueOn,
    bool IsGraded,
    bool IsOverdue,
    SubmissionDto? Submission);
