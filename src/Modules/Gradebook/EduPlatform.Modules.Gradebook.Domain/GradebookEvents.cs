using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.Gradebook.Domain;

public sealed record GradePublished(
    Guid GradeId,
    Guid StudentUserId,
    Guid TeachingAssignmentId,
    decimal Value) : DomainEvent;

public sealed record AbsenceRecorded(
    Guid AbsenceId,
    Guid StudentUserId,
    Guid TeachingAssignmentId,
    DateOnly Date) : DomainEvent;
