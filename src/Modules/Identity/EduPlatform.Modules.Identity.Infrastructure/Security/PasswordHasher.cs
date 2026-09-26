using EduPlatform.Modules.Identity.Application.Abstractions;
using EduPlatform.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;

namespace EduPlatform.Modules.Identity.Infrastructure.Security;

/// <summary>
/// PBKDF2-HMAC-SHA512 through ASP.NET Core Identity's hasher.
/// </summary>
/// <remarks>
/// The framework implementation is used rather than a hand-rolled one because it already
/// handles salt generation, constant-time comparison and a versioned format that allows the
/// iteration count to be raised later without invalidating existing passwords.
/// </remarks>
internal sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();

    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password must not be empty.", nameof(password));
        }

        // The hasher takes a user only to satisfy its generic signature; it does not read it.
        return _inner.HashPassword(user: null!, password);
    }

    public PasswordVerificationOutcome Verify(string hash, string password) =>
        _inner.VerifyHashedPassword(user: null!, hash, password) switch
        {
            PasswordVerificationResult.Success => PasswordVerificationOutcome.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerificationOutcome.SuccessRehashNeeded,
            _ => PasswordVerificationOutcome.Failed,
        };
}
