namespace EduPlatform.Modules.Gradebook.Contracts;

public sealed record GradeDto(
    Guid Id,
    Guid StudentUserId,
    string StudentName,
    Guid TeachingAssignmentId,
    string SubjectName,
    decimal Value,
    string Kind,
    string Reason,
    string? Comment,
    DateTimeOffset GivenAt);

public sealed record AbsenceDto(
    Guid Id,
    Guid StudentUserId,
    string StudentName,
    Guid TeachingAssignmentId,
    string SubjectName,
    DateOnly Date,
    int Period,
    string Kind,
    string? Note);

public sealed record SubjectPerformanceDto(
    Guid TeachingAssignmentId,
    string SubjectName,
    IReadOnlyList<GradeDto> Grades,
    decimal? Average,
    int UnexcusedAbsences,
    int ExcusedAbsences,
    int LateArrivals);

public sealed record StudentReportDto(
    Guid StudentUserId,
    string StudentName,
    IReadOnlyList<SubjectPerformanceDto> Subjects,
    decimal? OverallAverage);
