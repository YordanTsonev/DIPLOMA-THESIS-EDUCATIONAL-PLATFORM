using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.SchoolStructure.Domain;

public sealed class ParentChild : AggregateRoot
{
    private ParentChild(Guid id, Guid parentUserId, Guid studentUserId)
        : base(id)
    {
        ParentUserId = parentUserId;
        StudentUserId = studentUserId;
        LinkedAt = DateTimeOffset.UtcNow;
    }

    private ParentChild() { }

    public Guid ParentUserId { get; private set; }

    public Guid StudentUserId { get; private set; }

    public DateTimeOffset LinkedAt { get; private set; }

    public static ParentChild Link(Guid parentUserId, Guid studentUserId)
    {
        if (parentUserId == Guid.Empty)
        {
            throw new DomainException("A parent is required.");
        }

        if (studentUserId == Guid.Empty)
        {
            throw new DomainException("A student is required.");
        }

        if (parentUserId == studentUserId)
        {
            throw new DomainException("A user cannot be their own parent.");
        }

        return new ParentChild(Guid.CreateVersion7(), parentUserId, studentUserId);
    }
}
