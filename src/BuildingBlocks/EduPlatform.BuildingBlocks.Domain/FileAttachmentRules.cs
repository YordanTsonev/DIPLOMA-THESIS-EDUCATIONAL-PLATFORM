namespace EduPlatform.BuildingBlocks.Domain;

public static class FileAttachmentRules
{
    public const int MaxFileNameLength = 255;
    public const long MaxFileSizeBytes = 50L * 1024 * 1024;

    public static readonly string[] AllowedExtensions =
    [
        ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx",
        ".txt", ".png", ".jpg", ".jpeg", ".gif", ".zip", ".mp4",
    ];

    public static string Validate(string? fileName, long sizeBytes)
    {
        var name = fileName?.Trim() ?? string.Empty;

        if (name.Length == 0 || name.Length > MaxFileNameLength)
        {
            throw new DomainException("A valid file name is required.");
        }

        var extension = Path.GetExtension(name).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension, StringComparer.Ordinal))
        {
            throw new DomainException($"'{extension}' is not an allowed file type.");
        }

        if (sizeBytes <= 0)
        {
            throw new DomainException("The file is empty.");
        }

        if (sizeBytes > MaxFileSizeBytes)
        {
            throw new DomainException($"The file exceeds the {MaxFileSizeBytes / (1024 * 1024)} MB limit.");
        }

        return name;
    }
}
