using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.SchoolStructure.Domain;

public sealed record SchoolClassCreated(Guid SchoolClassId, Guid AcademicYearId, string DisplayName) : DomainEvent;

public sealed record StudentEnrolled(Guid SchoolClassId, Guid StudentUserId) : DomainEvent;

public sealed record StudentWithdrawn(Guid SchoolClassId, Guid StudentUserId) : DomainEvent;

public sealed record TeachingAssignmentCreated(
    Guid TeachingAssignmentId,
    Guid TeacherUserId,
    Guid SubjectId,
    Guid SchoolClassId) : DomainEvent;
