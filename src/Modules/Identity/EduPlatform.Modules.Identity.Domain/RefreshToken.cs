using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.Identity.Domain;

/// <summary>
/// One long-lived session belonging to a <see cref="User"/>.
/// </summary>
/// <remarks>
/// Only the <em>hash</em> of the token is stored. A leaked database therefore yields no usable
/// sessions, exactly as with passwords. Rows are kept after revocation because the history is
/// what makes replay detection possible.
/// </remarks>
public sealed class RefreshToken : Entity
{
    private RefreshToken(Guid userId, string tokenHash, DateTimeOffset expiresAt, string? deviceInfo)
        : base(Guid.CreateVersion7())
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        DeviceInfo = deviceInfo;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Required by EF Core materialisation.</summary>
    private RefreshToken() { }

    public Guid UserId { get; private init; }

    public string TokenHash { get; private init; } = null!;

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset ExpiresAt { get; private init; }

    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Why the token was revoked — rotation, sign-out, password change or replay.</summary>
    public string? RevokedReason { get; private set; }

    /// <summary>Hash of the token that superseded this one, when it was rotated normally.</summary>
    public string? ReplacedByTokenHash { get; private set; }

    /// <summary>Browser and platform, so a user can recognise their own sessions in a list.</summary>
    public string? DeviceInfo { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    internal static RefreshToken Issue(Guid userId, string tokenHash, DateTimeOffset expiresAt, string? deviceInfo)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainException("A refresh token hash is required.");
        }

        return new RefreshToken(userId, tokenHash, expiresAt, deviceInfo);
    }

    public bool HasExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool IsUsable(DateTimeOffset now) => !IsRevoked && !HasExpired(now);

    internal void Revoke(string reason)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAt = DateTimeOffset.UtcNow;
        RevokedReason = reason;
    }

    internal void ReplaceWith(string replacementTokenHash)
    {
        Revoke("rotated");
        ReplacedByTokenHash = replacementTokenHash;
    }
}
