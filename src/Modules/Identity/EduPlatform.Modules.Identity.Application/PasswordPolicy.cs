namespace EduPlatform.Modules.Identity.Application;

/// <summary>
/// The password rules, in one place so registration and reset cannot drift apart.
/// </summary>
/// <remarks>
/// Length is the requirement that actually matters; long composition rules mostly push people
/// towards "Parola1!" and a sticky note. A single letter-and-digit check plus a 10-character
/// minimum is the balance chosen here.
/// </remarks>
public static class PasswordPolicy
{
    public const int MinimumLength = 10;

    /// <summary>Guards against a denial of service through deliberately enormous inputs to a slow hash.</summary>
    public const int MaximumLength = 256;

    public static bool IsStrongEnough(string? password) =>
        !string.IsNullOrWhiteSpace(password)
        && password.Any(char.IsLetter)
        && password.Any(char.IsDigit);
}
