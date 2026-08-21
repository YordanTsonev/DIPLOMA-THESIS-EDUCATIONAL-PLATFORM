namespace EduPlatform.BuildingBlocks.Domain;

/// <summary>
/// Something that has happened inside the domain and other modules may care about.
/// Raised by an aggregate, dispatched after the transaction commits.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAt { get; }
}

/// <summary>Convenience base class for domain events.</summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
