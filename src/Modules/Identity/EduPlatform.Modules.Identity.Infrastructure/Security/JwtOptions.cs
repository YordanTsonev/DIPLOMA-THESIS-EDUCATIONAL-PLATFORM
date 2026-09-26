using System.ComponentModel.DataAnnotations;

namespace EduPlatform.Modules.Identity.Infrastructure.Security;

/// <summary>Signing and validation parameters for the access token.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Symmetric signing key. Comes from User Secrets in development and from a Kubernetes
    /// Secret in production; it must never sit in appsettings.json.
    /// </summary>
    [Required]
    [MinLength(32, ErrorMessage = "The signing key must be at least 32 characters (256 bits) long.")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Deliberately short. A revoked role or a deactivated account keeps working until the
    /// current access token expires, so this window is how long that gap can last.
    /// </summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);
}
