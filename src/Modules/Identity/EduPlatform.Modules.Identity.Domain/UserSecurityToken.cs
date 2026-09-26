using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.Identity.Domain;

/// <summary>
/// A single-use token sent to a user by e-mail.
/// </summary>
/// <remarks>
/// As with refresh tokens, only the hash is stored: the link in the mailbox is a bearer
/// credential, so a leaked database must not yield working links. Consumed tokens are kept
/// rather than deleted, because "this link was already used" is a more useful answer to the
/// user than "this link is invalid".
/// </remarks>
public sealed class UserSecurityToken : Entity
{
    private UserSecurityToken(Guid userId, UserTokenPurpose purpose, string tokenHash, DateTimeOffset expiresAt)
        : base(Guid.CreateVersion7())
    {
        UserId = userId;
        Purpose = purpose;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Required by EF Core materialisation.</summary>
    private UserSecurityToken() { }

    public Guid UserId { get; private init; }

    public UserTokenPurpose Purpose { get; private init; }

    public string TokenHash { get; private init; } = null!;

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset ExpiresAt { get; private init; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public bool IsConsumed => ConsumedAt is not null;

    internal static UserSecurityToken Issue(
        Guid userId,
        UserTokenPurpose purpose,
        string tokenHash,
        DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainException("A security token hash is required.");
        }

        return new UserSecurityToken(userId, purpose, tokenHash, expiresAt);
    }

    public bool HasExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool IsUsable(DateTimeOffset now) => !IsConsumed && !HasExpired(now);

    internal void Consume() => ConsumedAt = DateTimeOffset.UtcNow;
}
