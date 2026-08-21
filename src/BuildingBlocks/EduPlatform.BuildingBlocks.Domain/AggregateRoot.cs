namespace EduPlatform.BuildingBlocks.Domain;

/// <summary>
/// The entry point of a consistency boundary. Only aggregate roots are loaded and
/// saved as a unit, and only they raise domain events.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(Guid id) : base(id) { }

    protected AggregateRoot() { }

    /// <summary>Events raised since the aggregate was loaded, in the order they happened.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Called by the persistence layer once the events have been collected.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
