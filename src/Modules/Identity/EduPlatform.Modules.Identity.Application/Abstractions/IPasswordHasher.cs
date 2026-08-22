namespace EduPlatform.Modules.Identity.Application.Abstractions;

/// <summary>
/// Turns a plaintext password into a storable hash and verifies one against it.
/// </summary>
/// <remarks>
/// An interface rather than a direct call so the algorithm can be replaced without touching
/// any use case, and so tests can substitute a fast fake — a deliberately slow hash is correct
/// in production and intolerable in a test suite that creates users repeatedly.
/// </remarks>
public interface IPasswordHasher
{
    string Hash(string password);

    PasswordVerificationOutcome Verify(string hash, string password);
}

/// <summary>Result of checking a password against a stored hash.</summary>
public enum PasswordVerificationOutcome
{
    Failed = 0,

    Success = 1,

    /// <summary>
    /// Correct, but the stored hash uses outdated parameters. The caller should re-hash and
    /// save while it holds the plaintext — the only moment it is able to.
    /// </summary>
    SuccessRehashNeeded = 2,
}
