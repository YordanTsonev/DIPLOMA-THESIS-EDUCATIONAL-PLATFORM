namespace EduPlatform.Modules.Content.Contracts;

public sealed record MaterialFolderDto(Guid Id, Guid TeachingAssignmentId, string Name);

public sealed record MaterialDto(
    Guid Id,
    Guid TeachingAssignmentId,
    Guid? FolderId,
    string Kind,
    string Title,
    string? Description,
    string? FileName,
    string? ContentType,
    long? SizeBytes,
    string? Url,
    DateTimeOffset CreatedAt);
