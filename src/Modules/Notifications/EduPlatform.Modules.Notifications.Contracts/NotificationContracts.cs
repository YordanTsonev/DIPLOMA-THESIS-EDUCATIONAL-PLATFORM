namespace EduPlatform.Modules.Notifications.Contracts;

public sealed record NotificationDto(
    Guid Id,
    string Kind,
    string Title,
    string? Body,
    string? Link,
    DateTimeOffset CreatedAt,
    bool IsRead);

public sealed record NotificationFeedDto(IReadOnlyList<NotificationDto> Items, int UnreadCount);

public interface INotificationWriter
{
    Task SendAsync(
        Guid recipientUserId,
        string kind,
        string title,
        string? body = null,
        string? link = null,
        CancellationToken cancellationToken = default);

    Task SendManyAsync(
        IReadOnlyCollection<Guid> recipientUserIds,
        string kind,
        string title,
        string? body = null,
        string? link = null,
        CancellationToken cancellationToken = default);
}
