using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.Identity.Domain;

/// <summary>
/// A person who can sign in. This aggregate owns authentication only — who you are and whether
/// you may log in. What you are <em>within a school</em> (a student of class 9A, the teacher of
/// mathematics for three classes, the parent of two children) belongs to the SchoolStructure
/// module and is linked back here by <see cref="Entity.Id"/>.
/// </summary>
public sealed class User : AggregateRoot
{
    private readonly List<RefreshToken> _refreshTokens = [];
    private readonly List<UserSecurityToken> _securityTokens = [];

    private User(Guid id, Email email, PersonName name, string passwordHash, UserRole role)
        : base(id)
    {
        EmailAddress = email.Value;
        Name = name;
        PasswordHash = passwordHash;
        Role = role;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Required by EF Core materialisation.</summary>
    private User() { }

    /// <summary>
    /// The stored form of the address, mapped straight to the column.
    /// </summary>
    /// <remarks>
    /// The value object cannot be the mapped property. Stored through a value converter it is
    /// opaque to the provider, so neither a partial-match search nor a lookup by address can be
    /// translated to SQL. Keeping the string as the mapped property and deriving
    /// <see cref="Email"/> from it gives the domain its type and the database a plain column.
    /// </remarks>
    public string EmailAddress { get; private set; } = null!;

    /// <summary>The address as a validated value object.</summary>
    public Email Email => Email.Create(EmailAddress);

    public PersonName Name { get; private set; } = null!;

    /// <summary>Hash only. The plaintext password never leaves the request that created it.</summary>
    public string PasswordHash { get; private set; } = null!;

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; } = true;

    public bool EmailConfirmed { get; private set; }

    public string? PhoneNumber { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset? LastSignedInAt { get; private set; }

    /// <summary>Issued sessions, including revoked ones — the history is what detects token theft.</summary>
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    /// <summary>E-mail confirmation and password-reset tokens, including spent ones.</summary>
    public IReadOnlyCollection<UserSecurityToken> SecurityTokens => _securityTokens.AsReadOnly();

    public static User Register(Email email, PersonName name, string passwordHash, UserRole role)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(name);

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("A password hash is required.");
        }

        if (!Enum.IsDefined(role))
        {
            throw new DomainException($"'{role}' is not a known role.");
        }

        var user = new User(Guid.CreateVersion7(), email, name, passwordHash, role);
        user.Raise(new UserRegistered(user.Id, email.Value, role));
        return user;
    }

    public void ConfirmEmail()
    {
        if (EmailConfirmed)
        {
            return;
        }

        EmailConfirmed = true;
        Raise(new UserEmailConfirmed(Id, Email.Value));
    }

    public void ChangeRole(UserRole newRole)
    {
        if (!Enum.IsDefined(newRole))
        {
            throw new DomainException($"'{newRole}' is not a known role.");
        }

        if (newRole == Role)
        {
            return;
        }

        var previous = Role;
        Role = newRole;
        Raise(new UserRoleChanged(Id, previous, newRole));
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            throw new DomainException("A password hash is required.");
        }

        PasswordHash = newPasswordHash;

        // A password change ends every other session; that is the point of changing it.
        RevokeAllTokens("password changed");
    }

    public void UpdateProfile(PersonName name, string? phoneNumber)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        RevokeAllTokens("account deactivated");
        Raise(new UserDeactivated(Id));
    }

    public void Reactivate()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        Raise(new UserReactivated(Id));
    }

    public void RecordSignIn() => LastSignedInAt = DateTimeOffset.UtcNow;

    /// <summary>Starts a new session. Callers store only the hash of the token they hand out.</summary>
    public RefreshToken IssueRefreshToken(string tokenHash, DateTimeOffset expiresAt, string? deviceInfo = null)
    {
        if (!IsActive)
        {
            throw new DomainException("A deactivated account cannot be issued a session.");
        }

        var token = RefreshToken.Issue(Id, tokenHash, expiresAt, deviceInfo);
        _refreshTokens.Add(token);
        return token;
    }

    /// <summary>
    /// Exchanges a live token for a new one. If the presented token was already used, every
    /// session is revoked: replay means the token leaked, and the safe response is to end
    /// all sessions and force a fresh sign-in.
    /// </summary>
    public RefreshToken RotateRefreshToken(
        string presentedTokenHash,
        string replacementTokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset now,
        string? deviceInfo = null)
    {
        var existing = _refreshTokens.SingleOrDefault(token => token.TokenHash == presentedTokenHash)
            ?? throw new DomainException("Unknown refresh token.");

        if (existing.IsRevoked)
        {
            RevokeAllTokens("refresh token replay detected");
            throw new DomainException("This refresh token has already been used. All sessions were revoked.");
        }

        if (existing.HasExpired(now))
        {
            throw new DomainException("This refresh token has expired.");
        }

        var replacement = RefreshToken.Issue(Id, replacementTokenHash, expiresAt, deviceInfo ?? existing.DeviceInfo);
        _refreshTokens.Add(replacement);
        existing.ReplaceWith(replacement.TokenHash);
        return replacement;
    }

    public void RevokeAllTokens(string reason)
    {
        foreach (var token in _refreshTokens.Where(token => !token.IsRevoked))
        {
            token.Revoke(reason);
        }
    }

    /// <summary>
    /// Issues a single-use token for an e-mailed link. Any earlier token for the same purpose is
    /// spent immediately, so requesting a second password-reset mail invalidates the first link.
    /// </summary>
    public UserSecurityToken IssueSecurityToken(UserTokenPurpose purpose, string tokenHash, DateTimeOffset expiresAt)
    {
        foreach (var outstanding in _securityTokens.Where(token => token.Purpose == purpose && !token.IsConsumed))
        {
            outstanding.Consume();
        }

        var token = UserSecurityToken.Issue(Id, purpose, tokenHash, expiresAt);
        _securityTokens.Add(token);
        return token;
    }

    /// <summary>Spends a token, refusing one that is unknown, already used or expired.</summary>
    public UserSecurityToken ConsumeSecurityToken(UserTokenPurpose purpose, string tokenHash, DateTimeOffset now)
    {
        var token = _securityTokens.SingleOrDefault(
                candidate => candidate.Purpose == purpose && candidate.TokenHash == tokenHash)
            ?? throw new DomainException("This link is not valid.");

        if (token.IsConsumed)
        {
            throw new DomainException("This link has already been used.");
        }

        if (token.HasExpired(now))
        {
            throw new DomainException("This link has expired. Request a new one.");
        }

        token.Consume();
        return token;
    }

    /// <summary>
    /// Sets a new password from a reset link. Separate from <see cref="ChangePassword"/> because
    /// a reset also confirms the address: the link could only have been read from that mailbox.
    /// </summary>
    public void ResetPassword(string newPasswordHash)
    {
        ChangePassword(newPasswordHash);
        ConfirmEmail();
    }
}
