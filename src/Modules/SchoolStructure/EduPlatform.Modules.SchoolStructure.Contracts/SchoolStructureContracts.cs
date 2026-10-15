namespace EduPlatform.Modules.SchoolStructure.Contracts;

public sealed record AcademicYearDto(
    Guid Id,
    string Name,
    DateOnly StartsOn,
    DateOnly EndsOn,
    bool IsCurrent);

public sealed record SubjectDto(Guid Id, string Name, string Code, bool IsActive);

public sealed record SchoolClassDto(
    Guid Id,
    Guid AcademicYearId,
    string AcademicYearName,
    int Grade,
    string Section,
    string DisplayName,
    Guid? HomeroomTeacherId,
    string? HomeroomTeacherName,
    int StudentCount);

public sealed record ClassStudentDto(
    Guid StudentUserId,
    string FullName,
    string Email,
    DateTimeOffset EnrolledAt);

public sealed record TeachingAssignmentDto(
    Guid Id,
    Guid TeacherUserId,
    string TeacherName,
    Guid SubjectId,
    string SubjectName,
    Guid SchoolClassId,
    string SchoolClassName,
    int WeeklyHours);

public sealed record ScheduleSlotDto(
    Guid Id,
    Guid TeachingAssignmentId,
    DayOfWeek DayOfWeek,
    int Period,
    TimeOnly StartsAt,
    TimeOnly EndsAt,
    string? Room,
    Guid SubjectId,
    string SubjectName,
    Guid SchoolClassId,
    string SchoolClassName,
    Guid TeacherUserId,
    string TeacherName);
