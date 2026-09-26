namespace EduPlatform.Modules.Identity.Domain;

/// <summary>What a single-use security token entitles its bearer to do.</summary>
public enum UserTokenPurpose
{
    /// <summary>Proves the address in the mailbox belongs to the person who registered.</summary>
    EmailConfirmation = 1,

    /// <summary>Allows setting a new password without knowing the old one.</summary>
    PasswordReset = 2,
}
