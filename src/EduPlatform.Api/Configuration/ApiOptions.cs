using System.ComponentModel.DataAnnotations;

namespace EduPlatform.Api.Configuration;

/// <summary>Cross-origin settings for the Angular client.</summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>Origins allowed to call the API with credentials.</summary>
    [Required]
    [MinLength(1)]
    public string[] AllowedOrigins { get; init; } = [];
}

/// <summary>Object storage (MinIO in development, S3-compatible in production).</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    [Required]
    public string Endpoint { get; init; } = string.Empty;

    [Required]
    public string AccessKey { get; init; } = string.Empty;

    [Required]
    public string SecretKey { get; init; } = string.Empty;

    public bool UseSsl { get; init; }
}
