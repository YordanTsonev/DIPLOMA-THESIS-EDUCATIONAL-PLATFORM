using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.BuildingBlocks.Events;

/// <summary>
/// Reacts to something that has already happened. Unlike a command handler there may be
/// many handlers for one event, and none of them can veto it.
/// </summary>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
