using EduPlatform.Modules.Notifications.Application.Abstractions;
using EduPlatform.Modules.Notifications.Contracts;
using EduPlatform.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Modules.Notifications.Infrastructure.Persistence;

internal sealed class NotificationRepository(NotificationsDbContext dbContext) : INotificationRepository
{
    public Task<Notification?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Notifications.SingleOrDefaultAsync(notification => notification.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Notification>> ListUnreadAsync(
        Guid recipientUserId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Notifications
            .Where(notification => notification.RecipientUserId == recipientUserId && notification.ReadAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public void Add(Notification notification) => dbContext.Notifications.Add(notification);

    public void AddRange(IReadOnlyCollection<Notification> notifications) =>
        dbContext.Notifications.AddRange(notifications);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

internal sealed class NotificationReadStore(NotificationsDbContext dbContext) : INotificationReadStore
{
    public async Task<NotificationFeedDto> ListForUserAsync(
        Guid recipientUserId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var items = await dbContext.Notifications
            .AsNoTracking()
            .Where(notification => notification.RecipientUserId == recipientUserId)
            .OrderByDescending(notification => notification.CreatedAt)
            .Take(limit)
            .Select(notification => new NotificationDto(
                notification.Id,
                notification.Kind.ToString(),
                notification.Title,
                notification.Body,
                notification.Link,
                notification.CreatedAt,
                notification.ReadAt != null))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var unread = await CountUnreadAsync(recipientUserId, cancellationToken).ConfigureAwait(false);

        return new NotificationFeedDto(items, unread);
    }

    public Task<int> CountUnreadAsync(Guid recipientUserId, CancellationToken cancellationToken = default) =>
        dbContext.Notifications.CountAsync(
            notification => notification.RecipientUserId == recipientUserId && notification.ReadAt == null,
            cancellationToken);
}

internal sealed class NotificationWriter(NotificationsDbContext dbContext) : INotificationWriter
{
    public async Task SendAsync(
        Guid recipientUserId,
        string kind,
        string title,
        string? body = null,
        string? link = null,
        CancellationToken cancellationToken = default)
    {
        dbContext.Notifications.Add(Notification.Create(recipientUserId, Parse(kind), title, body, link));
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SendManyAsync(
        IReadOnlyCollection<Guid> recipientUserIds,
        string kind,
        string title,
        string? body = null,
        string? link = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recipientUserIds);

        if (recipientUserIds.Count == 0)
        {
            return;
        }

        var parsed = Parse(kind);

        foreach (var recipientUserId in recipientUserIds.Distinct())
        {
            dbContext.Notifications.Add(Notification.Create(recipientUserId, parsed, title, body, link));
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static NotificationKind Parse(string kind) =>
        Enum.TryParse<NotificationKind>(kind, out var parsed) ? parsed : NotificationKind.Announcement;
}
