using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.BuildingBlocks.Events;

/// <summary>
/// Delivers domain events to their handlers.
/// </summary>
/// <remarks>
/// Called <em>after</em> the database transaction commits. Events describe facts, so a
/// handler that fails must not roll back the change that produced it — from Phase 7 the
/// delivery is backed by the transactional outbox for exactly this reason.
/// </remarks>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
