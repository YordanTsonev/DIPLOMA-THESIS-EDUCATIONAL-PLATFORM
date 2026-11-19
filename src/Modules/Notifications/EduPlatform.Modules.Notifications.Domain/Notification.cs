using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.Notifications.Domain;

public enum NotificationKind
{
    Grade = 1,
    Homework = 2,
    Exam = 3,
    Absence = 4,
    Announcement = 5,
}

public sealed class Notification : AggregateRoot
{
    public const int MaxTitleLength = 160;
    public const int MaxBodyLength = 1000;
    public const int MaxLinkLength = 300;

    private Notification(
        Guid id,
        Guid recipientUserId,
        NotificationKind kind,
        string title,
        string? body,
        string? link)
        : base(id)
    {
        RecipientUserId = recipientUserId;
        Kind = kind;
        Title = title;
        Body = body;
        Link = link;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private Notification() { }

    public Guid RecipientUserId { get; private set; }

    public NotificationKind Kind { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Body { get; private set; }

    public string? Link { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public bool IsRead => ReadAt is not null;

    public static Notification Create(
        Guid recipientUserId,
        NotificationKind kind,
        string title,
        string? body = null,
        string? link = null)
    {
        if (recipientUserId == Guid.Empty)
        {
            throw new DomainException("A recipient is required.");
        }

        if (!Enum.IsDefined(kind))
        {
            throw new DomainException("Unknown notification kind.");
        }

        return new Notification(
            Guid.CreateVersion7(),
            recipientUserId,
            kind,
            Require(title, MaxTitleLength, "A title"),
            Optional(body, MaxBodyLength, "The body"),
            Optional(link, MaxLinkLength, "The link"));
    }

    public void MarkRead()
    {
        ReadAt ??= DateTimeOffset.UtcNow;
    }

    private static string Require(string value, int maxLength, string what)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            throw new DomainException($"{what} is required.");
        }

        return trimmed.Length > maxLength
            ? throw new DomainException($"{what} must not exceed {maxLength} characters.")
            : trimmed;
    }

    private static string? Optional(string? value, int maxLength, string what)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length > maxLength
            ? throw new DomainException($"{what} must not exceed {maxLength} characters.")
            : trimmed;
    }
}
