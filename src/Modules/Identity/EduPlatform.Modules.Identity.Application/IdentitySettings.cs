namespace EduPlatform.Modules.Identity.Application;

/// <summary>Lifetimes and addresses the authentication flows depend on.</summary>
public sealed class IdentitySettings
{
    public const string SectionName = "Identity";

    /// <summary>
    /// How long a refresh token stays valid. Long enough that a student is not asked to sign in
    /// during a school term, short enough that an abandoned session eventually dies.
    /// </summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(14);

    /// <summary>Generous, because confirmation mail is often read a day or two later.</summary>
    public TimeSpan EmailConfirmationLifetime { get; set; } = TimeSpan.FromDays(3);

    /// <summary>Deliberately short: a reset link is the strongest credential the system mails out.</summary>
    public TimeSpan PasswordResetLifetime { get; set; } = TimeSpan.FromHours(2);

    /// <summary>Base address of the Angular client, used to build the links inside e-mails.</summary>
    public string ClientBaseUrl { get; set; } = "http://localhost:4200";

    public string FromAddress { get; set; } = "no-reply@eduplatform.local";

    public string FromName { get; set; } = "EduPlatform";
}
