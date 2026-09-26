using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.Identity.Domain;

/// <summary>
/// One recorded security-relevant action.
/// </summary>
/// <remarks>
/// Append-only by design: rows are never updated or deleted, which is what makes the log
/// worth consulting after an incident. Sign-in failures are recorded alongside successes,
/// because a burst of failures against one account is the signal worth seeing.
/// </remarks>
public sealed class AuditEntry : Entity
{
    private AuditEntry(Guid? actorUserId, string action, Guid? subjectUserId, string? detail, string? ipAddress)
        : base(Guid.CreateVersion7())
    {
        ActorUserId = actorUserId;
        Action = action;
        SubjectUserId = subjectUserId;
        Detail = detail;
        IpAddress = ipAddress;
        OccurredAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Required by EF Core materialisation.</summary>
    private AuditEntry() { }

    /// <summary>Who performed the action. Null for anonymous attempts such as a failed sign-in.</summary>
    public Guid? ActorUserId { get; private init; }

    /// <summary>Stable action name, for example <c>user.role_changed</c>.</summary>
    public string Action { get; private init; } = null!;

    /// <summary>Whose account the action concerned, when that differs from the actor.</summary>
    public Guid? SubjectUserId { get; private init; }

    public string? Detail { get; private init; }

    public string? IpAddress { get; private init; }

    public DateTimeOffset OccurredAt { get; private init; }

    public static AuditEntry Record(
        string action,
        Guid? actorUserId = null,
        Guid? subjectUserId = null,
        string? detail = null,
        string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new DomainException("An audit action name is required.");
        }

        return new AuditEntry(actorUserId, action, subjectUserId, detail, ipAddress);
    }
}

/// <summary>The action names this module records. Constants, so a typo cannot create a new kind of event.</summary>
public static class AuditActions
{
    public const string UserRegistered = "user.registered";
    public const string SignInSucceeded = "user.sign_in_succeeded";
    public const string SignInFailed = "user.sign_in_failed";
    public const string SignedOut = "user.signed_out";
    public const string SessionRefreshed = "user.session_refreshed";
    public const string RefreshTokenReplayDetected = "user.refresh_token_replay_detected";
    public const string EmailConfirmed = "user.email_confirmed";
    public const string PasswordResetRequested = "user.password_reset_requested";
    public const string PasswordReset = "user.password_reset";
    public const string RoleChanged = "user.role_changed";
    public const string Deactivated = "user.deactivated";
    public const string Reactivated = "user.reactivated";
}
