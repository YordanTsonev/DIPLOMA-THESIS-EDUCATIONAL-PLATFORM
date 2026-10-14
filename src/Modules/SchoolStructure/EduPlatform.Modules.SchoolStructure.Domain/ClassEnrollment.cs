using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.SchoolStructure.Domain;

public sealed class ClassEnrollment : Entity
{
    private ClassEnrollment(Guid schoolClassId, Guid studentUserId)
        : base(Guid.CreateVersion7())
    {
        SchoolClassId = schoolClassId;
        StudentUserId = studentUserId;
        EnrolledAt = DateTimeOffset.UtcNow;
    }

    private ClassEnrollment() { }

    public Guid SchoolClassId { get; private set; }

    public Guid StudentUserId { get; private set; }

    public DateTimeOffset EnrolledAt { get; private set; }

    public DateTimeOffset? WithdrawnAt { get; private set; }

    public bool IsActive => WithdrawnAt is null;

    internal static ClassEnrollment Create(Guid schoolClassId, Guid studentUserId) =>
        new(schoolClassId, studentUserId);

    internal void Withdraw() => WithdrawnAt = DateTimeOffset.UtcNow;

    internal void Restore()
    {
        WithdrawnAt = null;
        EnrolledAt = DateTimeOffset.UtcNow;
    }
}
