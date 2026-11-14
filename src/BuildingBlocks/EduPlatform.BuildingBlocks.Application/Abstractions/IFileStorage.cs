namespace EduPlatform.BuildingBlocks.Application.Abstractions;

public static class StorageBuckets
{
    public const string Materials = "materials";

    public const string Submissions = "submissions";
}

public interface IFileStorage
{
    Task<string> SaveAsync(
        string bucket,
        Stream content,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenAsync(string bucket, string storageKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string bucket, string storageKey, CancellationToken cancellationToken = default);
}
