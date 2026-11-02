using EduPlatform.Modules.Content.Application.Abstractions;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace EduPlatform.Modules.Content.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Endpoint { get; set; } = "localhost:9000";

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public bool UseSsl { get; set; }

    public string MaterialsBucket { get; set; } = "materials";
}

internal sealed class MinioFileStorage : IFileStorage
{
    private readonly IMinioClient _client;
    private readonly StorageOptions _options;

    public MinioFileStorage(IOptions<StorageOptions> options)
    {
        _options = options.Value;

        _client = new MinioClient()
            .WithEndpoint(_options.Endpoint)
            .WithCredentials(_options.AccessKey, _options.SecretKey)
            .WithSSL(_options.UseSsl)
            .Build();
    }

    public async Task<string> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        var key = $"{DateTime.UtcNow:yyyy/MM}/{Guid.CreateVersion7()}{Path.GetExtension(fileName)}";

        await _client.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(_options.MaterialsBucket)
                .WithObject(key)
                .WithStreamData(content)
                .WithObjectSize(sizeBytes)
                .WithContentType(contentType),
            cancellationToken).ConfigureAwait(false);

        return key;
    }

    public async Task<Stream> OpenAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var buffer = new MemoryStream();

        await _client.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(_options.MaterialsBucket)
                .WithObject(storageKey)
                .WithCallbackStream(stream => stream.CopyTo(buffer)),
            cancellationToken).ConfigureAwait(false);

        buffer.Position = 0;
        return buffer;
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default) =>
        await _client.RemoveObjectAsync(
            new RemoveObjectArgs()
                .WithBucket(_options.MaterialsBucket)
                .WithObject(storageKey),
            cancellationToken).ConfigureAwait(false);
}
