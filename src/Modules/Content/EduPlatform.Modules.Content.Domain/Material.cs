using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.Content.Domain;

public enum MaterialKind
{
    File = 1,
    Link = 2,
}

public sealed class Material : AggregateRoot
{
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 1000;
    public const int MaxUrlLength = 2000;
    public const int MaxFileNameLength = 255;
    public const long MaxFileSizeBytes = 50L * 1024 * 1024;

    public static readonly string[] AllowedExtensions =
    [
        ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx",
        ".txt", ".png", ".jpg", ".jpeg", ".gif", ".zip", ".mp4",
    ];

    private Material(
        Guid id,
        Guid teachingAssignmentId,
        Guid? folderId,
        MaterialKind kind,
        string title,
        string? description,
        Guid uploadedByUserId)
        : base(id)
    {
        TeachingAssignmentId = teachingAssignmentId;
        FolderId = folderId;
        Kind = kind;
        Title = title;
        Description = description;
        UploadedByUserId = uploadedByUserId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private Material() { }

    public Guid TeachingAssignmentId { get; private set; }

    public Guid? FolderId { get; private set; }

    public MaterialKind Kind { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public string? StorageKey { get; private set; }

    public string? FileName { get; private set; }

    public string? ContentType { get; private set; }

    public long? SizeBytes { get; private set; }

    public string? Url { get; private set; }

    public Guid UploadedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Material CreateFile(
        Guid teachingAssignmentId,
        Guid? folderId,
        string? title,
        string? description,
        string storageKey,
        string? fileName,
        string? contentType,
        long sizeBytes,
        Guid uploadedByUserId)
    {
        var name = fileName?.Trim() ?? string.Empty;

        if (name.Length == 0 || name.Length > MaxFileNameLength)
        {
            throw new DomainException("A valid file name is required.");
        }

        var extension = Path.GetExtension(name).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension, StringComparer.Ordinal))
        {
            throw new DomainException($"Files of type {extension} are not accepted.");
        }

        if (sizeBytes <= 0)
        {
            throw new DomainException("The file is empty.");
        }

        if (sizeBytes > MaxFileSizeBytes)
        {
            throw new DomainException($"The file exceeds the {MaxFileSizeBytes / (1024 * 1024)} MB limit.");
        }

        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new DomainException("A storage key is required.");
        }

        var material = new Material(
            Guid.CreateVersion7(),
            RequireAssignment(teachingAssignmentId),
            folderId,
            MaterialKind.File,
            NormaliseTitle(title, name),
            NormaliseDescription(description),
            RequireUser(uploadedByUserId));

        material.StorageKey = storageKey;
        material.FileName = name;
        material.ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
        material.SizeBytes = sizeBytes;

        return material;
    }

    public static Material CreateLink(
        Guid teachingAssignmentId,
        Guid? folderId,
        string? title,
        string? description,
        string? url,
        Guid uploadedByUserId)
    {
        var address = url?.Trim() ?? string.Empty;

        if (address.Length == 0 || address.Length > MaxUrlLength)
        {
            throw new DomainException("A valid address is required.");
        }

        if (!Uri.TryCreate(address, UriKind.Absolute, out var parsed)
            || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
        {
            throw new DomainException("The address must start with http or https.");
        }

        var material = new Material(
            Guid.CreateVersion7(),
            RequireAssignment(teachingAssignmentId),
            folderId,
            MaterialKind.Link,
            NormaliseTitle(title, address),
            NormaliseDescription(description),
            RequireUser(uploadedByUserId));

        material.Url = address;
        return material;
    }

    public void MoveTo(Guid? folderId) => FolderId = folderId;

    public void Rename(string? title) => Title = NormaliseTitle(title, Title);

    private static Guid RequireAssignment(Guid teachingAssignmentId) =>
        teachingAssignmentId == Guid.Empty
            ? throw new DomainException("A teaching assignment is required.")
            : teachingAssignmentId;

    private static Guid RequireUser(Guid userId) =>
        userId == Guid.Empty ? throw new DomainException("An uploader is required.") : userId;

    private static string NormaliseTitle(string? title, string fallback)
    {
        var trimmed = title?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            trimmed = fallback;
        }

        return trimmed.Length > MaxTitleLength ? trimmed[..MaxTitleLength] : trimmed;
    }

    private static string? NormaliseDescription(string? description)
    {
        var trimmed = description?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length > MaxDescriptionLength
            ? throw new DomainException($"Description must not exceed {MaxDescriptionLength} characters.")
            : trimmed;
    }
}
