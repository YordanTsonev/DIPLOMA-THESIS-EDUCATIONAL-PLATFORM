using System.Collections.Concurrent;
using EduPlatform.BuildingBlocks.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EduPlatform.BuildingBlocks.Events;

/// <summary>
/// In-process dispatcher. Resolves every handler registered for an event's runtime type
/// and invokes them in sequence.
/// </summary>
internal sealed partial class DomainEventDispatcher(
    IServiceProvider services,
    ILogger<DomainEventDispatcher> logger)
    : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, EventWrapper> Wrappers = new();

    public async Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            var wrapper = Wrappers.GetOrAdd(
                domainEvent.GetType(),
                static eventType => (EventWrapper)Activator.CreateInstance(
                    typeof(EventWrapper<>).MakeGenericType(eventType))!);

            Dispatching(logger, domainEvent.GetType().Name, domainEvent.EventId);
            await wrapper.DispatchAsync(domainEvent, services, cancellationToken).ConfigureAwait(false);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Dispatching domain event {EventName} ({EventId})")]
    private static partial void Dispatching(ILogger logger, string eventName, Guid eventId);

    private abstract class EventWrapper
    {
        public abstract Task DispatchAsync(
            IDomainEvent domainEvent,
            IServiceProvider services,
            CancellationToken cancellationToken);
    }

    private sealed class EventWrapper<TEvent> : EventWrapper
        where TEvent : IDomainEvent
    {
        public override async Task DispatchAsync(
            IDomainEvent domainEvent,
            IServiceProvider services,
            CancellationToken cancellationToken)
        {
            var typed = (TEvent)domainEvent;

            foreach (var handler in services.GetServices<IDomainEventHandler<TEvent>>())
            {
                await handler.HandleAsync(typed, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
