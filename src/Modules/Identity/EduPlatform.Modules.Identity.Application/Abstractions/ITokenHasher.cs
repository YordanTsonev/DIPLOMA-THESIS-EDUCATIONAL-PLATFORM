namespace EduPlatform.Modules.Identity.Application.Abstractions;

/// <summary>
/// Generates high-entropy bearer tokens and hashes them for storage.
/// </summary>
/// <remarks>
/// Separate from <see cref="IPasswordHasher"/> on purpose. Passwords are low-entropy and need a
/// deliberately slow algorithm; these tokens are 256 random bits, where a single SHA-256 is both
/// sufficient and fast enough to run on every refresh request.
/// </remarks>
public interface ITokenHasher
{
    /// <summary>Returns a new random token and its hash. The plaintext is shown to the user once.</summary>
    (string Token, string Hash) Generate();

    string Hash(string token);
}
