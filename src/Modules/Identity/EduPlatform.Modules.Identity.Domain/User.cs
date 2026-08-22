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

    private User(Guid id, Email email, PersonName name, string passwordHash, UserRole role)
        : base(id)
    {
        Email = email;
        Name = name;
        PasswordHash = passwordHash;
        Role = role;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Required by EF Core materialisation.</summary>
    private User() { }

    public Email Email { get; private set; } = null!;

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
}
