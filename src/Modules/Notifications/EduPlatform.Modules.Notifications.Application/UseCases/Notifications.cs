using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Notifications.Application.Abstractions;
using EduPlatform.Modules.Notifications.Contracts;

namespace EduPlatform.Modules.Notifications.Application.UseCases;

public sealed record ListMyNotificationsQuery(Guid UserId, int Limit = 30) : IQuery<NotificationFeedDto>;

internal sealed class ListMyNotificationsHandler(INotificationReadStore store)
    : IQueryHandler<ListMyNotificationsQuery, NotificationFeedDto>
{
    public Task<NotificationFeedDto> HandleAsync(
        ListMyNotificationsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return store.ListForUserAsync(query.UserId, Math.Clamp(query.Limit, 1, 100), cancellationToken);
    }
}

public sealed record MarkNotificationReadCommand(Guid NotificationId, Guid UserId) : ICommand;

internal sealed class MarkNotificationReadHandler(INotificationRepository repository)
    : CommandHandler<MarkNotificationReadCommand>
{
    protected override async Task HandleCoreAsync(
        MarkNotificationReadCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var notification = await repository.FindAsync(command.NotificationId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new DomainException("Notification not found.");

        if (notification.RecipientUserId != command.UserId)
        {
            throw new ForbiddenException("This notification is not yours.");
        }

        notification.MarkRead();
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed record MarkAllNotificationsReadCommand(Guid UserId) : ICommand;

internal sealed class MarkAllNotificationsReadHandler(INotificationRepository repository)
    : CommandHandler<MarkAllNotificationsReadCommand>
{
    protected override async Task HandleCoreAsync(
        MarkAllNotificationsReadCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var unread = await repository.ListUnreadAsync(command.UserId, cancellationToken).ConfigureAwait(false);

        if (unread.Count == 0)
        {
            return;
        }

        foreach (var notification in unread)
        {
            notification.MarkRead();
        }

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
