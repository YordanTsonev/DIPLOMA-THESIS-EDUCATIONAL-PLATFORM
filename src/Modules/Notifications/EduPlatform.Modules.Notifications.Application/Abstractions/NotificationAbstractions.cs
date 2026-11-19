using EduPlatform.Modules.Notifications.Contracts;
using EduPlatform.Modules.Notifications.Domain;

namespace EduPlatform.Modules.Notifications.Application.Abstractions;

public interface INotificationRepository
{
    Task<Notification?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> ListUnreadAsync(
        Guid recipientUserId,
        CancellationToken cancellationToken = default);

    void Add(Notification notification);

    void AddRange(IReadOnlyCollection<Notification> notifications);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface INotificationReadStore
{
    Task<NotificationFeedDto> ListForUserAsync(
        Guid recipientUserId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(Guid recipientUserId, CancellationToken cancellationToken = default);
}
