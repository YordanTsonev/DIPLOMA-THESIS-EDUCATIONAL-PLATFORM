namespace EduPlatform.Modules.Assessment.Contracts;

public sealed record QuestionOptionDto(Guid Id, string Text, bool IsCorrect, int Position);

public sealed record QuestionDto(
    Guid Id,
    Guid SubjectId,
    string SubjectName,
    string Kind,
    string Text,
    string? Explanation,
    Guid AuthorUserId,
    string AuthorName,
    IReadOnlyList<QuestionOptionDto> Options);

public sealed record ExamItemDto(Guid QuestionId, int Points, int Position, QuestionDto Question);

public sealed record ExamDto(
    Guid Id,
    Guid TeachingAssignmentId,
    string SubjectName,
    string SchoolClassName,
    string Title,
    string? Description,
    int DurationMinutes,
    DateTimeOffset? OpensAt,
    DateTimeOffset? ClosesAt,
    bool IsPublished,
    int MaxScore,
    int QuestionCount,
    int AttemptCount,
    int AwaitingReview,
    IReadOnlyList<ExamItemDto> Items);

public sealed record ExamOptionDto(Guid Id, string Text);

public sealed record ExamQuestionDto(
    Guid Id,
    string Kind,
    string Text,
    int Points,
    IReadOnlyList<ExamOptionDto> Options,
    string? GivenText,
    IReadOnlyList<Guid> SelectedOptionIds);

public sealed record AttemptSheetDto(
    Guid AttemptId,
    Guid ExamId,
    string Title,
    string? Description,
    string SubjectName,
    DateTimeOffset Deadline,
    int SecondsLeft,
    bool IsSubmitted,
    IReadOnlyList<ExamQuestionDto> Questions);

public sealed record AttemptAnswerDto(
    Guid QuestionId,
    string QuestionText,
    string Kind,
    int Points,
    int? AwardedPoints,
    string? Feedback,
    string? GivenText,
    IReadOnlyList<Guid> SelectedOptionIds,
    IReadOnlyList<QuestionOptionDto> Options);

public sealed record AttemptDto(
    Guid Id,
    Guid ExamId,
    string ExamTitle,
    Guid StudentUserId,
    string StudentName,
    DateTimeOffset StartedAt,
    DateTimeOffset Deadline,
    DateTimeOffset? SubmittedAt,
    int? Score,
    int MaxScore,
    decimal? Mark,
    decimal SuggestedMark,
    bool IsFullyScored,
    bool IsReleased,
    Guid? GradeId,
    IReadOnlyList<AttemptAnswerDto> Answers);

public sealed record StudentExamDto(
    Guid ExamId,
    string Title,
    string SubjectName,
    int DurationMinutes,
    int QuestionCount,
    int MaxScore,
    DateTimeOffset? OpensAt,
    DateTimeOffset? ClosesAt,
    bool IsOpen,
    Guid? AttemptId,
    bool IsSubmitted,
    int? Score,
    decimal? Mark,
    bool IsReleased);

public sealed record ExamSummaryDto(
    Guid Id,
    Guid TeachingAssignmentId,
    string Title,
    bool IsPublished,
    int DurationMinutes,
    int QuestionCount,
    int MaxScore);

public sealed record AttemptSummaryDto(
    Guid AttemptId,
    bool IsSubmitted,
    int? Score,
    int MaxScore,
    decimal? Mark,
    bool IsReleased);

public interface IExamDirectory
{
    Task<ExamSummaryDto?> FindExamSummaryAsync(Guid examId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExamSummaryDto>> ListPublishedAsync(
        Guid teachingAssignmentId,
        CancellationToken cancellationToken = default);

    Task<AttemptSummaryDto?> FindAttemptAsync(
        Guid examId,
        Guid studentUserId,
        CancellationToken cancellationToken = default);
}
