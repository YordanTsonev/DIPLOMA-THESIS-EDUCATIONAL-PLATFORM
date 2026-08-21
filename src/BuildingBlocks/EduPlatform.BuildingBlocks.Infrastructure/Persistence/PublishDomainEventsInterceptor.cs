using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.BuildingBlocks.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EduPlatform.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Collects the domain events raised by aggregates during a unit of work and dispatches
/// them once the transaction has committed.
/// </summary>
/// <remarks>
/// Dispatching happens in <c>SavedChanges</c>, not <c>SavingChanges</c>: an event states
/// that something <em>has</em> happened, so it must not be published while the write can
/// still roll back.
/// </remarks>
public sealed class PublishDomainEventsInterceptor(IDomainEventDispatcher dispatcher)
    : SaveChangesInterceptor
{
    private readonly List<IDomainEvent> _pending = [];

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        if (eventData.Context is not null)
        {
            CollectEvents(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_pending.Count > 0)
        {
            var events = _pending.ToArray();
            _pending.Clear();
            await dispatcher.DispatchAsync(events, cancellationToken).ConfigureAwait(false);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    private void CollectEvents(DbContext context)
    {
        var roots = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity)
            .ToArray();

        foreach (var root in roots)
        {
            _pending.AddRange(root.DomainEvents);
            root.ClearDomainEvents();
        }
    }
}
