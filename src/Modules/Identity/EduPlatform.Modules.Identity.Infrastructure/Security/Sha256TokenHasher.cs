using System.Security.Cryptography;
using System.Text;
using EduPlatform.Modules.Identity.Application.Abstractions;

namespace EduPlatform.Modules.Identity.Infrastructure.Security;

/// <summary>
/// Generates 256-bit random tokens and stores them as a SHA-256 hex digest.
/// </summary>
/// <remarks>
/// A single fast hash is the right choice here, unlike for passwords. The input is 256 bits from
/// a cryptographic RNG, so there is no dictionary to try and nothing for a slow KDF to defend
/// against — while refresh runs on a hot path where a deliberately slow hash would be felt.
/// </remarks>
internal sealed class Sha256TokenHasher : ITokenHasher
{
    private const int TokenBytes = 32;

    public (string Token, string Hash) Generate()
    {
        // URL-safe, because these tokens travel inside e-mailed links.
        var token = Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenBytes));
        return (token, Hash(token));
    }

    public string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexStringLower(digest);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
