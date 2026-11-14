using EduPlatform.BuildingBlocks.Application.Abstractions;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace EduPlatform.BuildingBlocks.Infrastructure.Storage;

public sealed class FileStorageOptions
{
    public const string SectionName = "Storage";

    public string Endpoint { get; set; } = "localhost:9000";

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public bool UseSsl { get; set; }
}

internal sealed class MinioFileStorage : IFileStorage
{
    private readonly IMinioClient _client;

    public MinioFileStorage(IOptions<FileStorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var settings = options.Value;

        _client = new MinioClient()
            .WithEndpoint(settings.Endpoint)
            .WithCredentials(settings.AccessKey, settings.SecretKey)
            .WithSSL(settings.UseSsl)
            .Build();
    }

    public async Task<string> SaveAsync(
        string bucket,
        Stream content,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        var key = $"{DateTime.UtcNow:yyyy/MM}/{Guid.CreateVersion7()}{Path.GetExtension(fileName)}";

        await _client.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(key)
                .WithStreamData(content)
                .WithObjectSize(sizeBytes)
                .WithContentType(contentType),
            cancellationToken).ConfigureAwait(false);

        return key;
    }

    public async Task<Stream> OpenAsync(
        string bucket,
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var buffer = new MemoryStream();

        await _client.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(bucket)
                .WithObject(storageKey)
                .WithCallbackStream(stream => stream.CopyTo(buffer)),
            cancellationToken).ConfigureAwait(false);

        buffer.Position = 0;
        return buffer;
    }

    public async Task DeleteAsync(
        string bucket,
        string storageKey,
        CancellationToken cancellationToken = default) =>
        await _client.RemoveObjectAsync(
            new RemoveObjectArgs()
                .WithBucket(bucket)
                .WithObject(storageKey),
            cancellationToken).ConfigureAwait(false);
}
