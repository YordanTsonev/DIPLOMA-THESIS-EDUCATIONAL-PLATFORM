using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.Modules.Identity.Application.Abstractions;
using EduPlatform.Modules.Identity.Domain;

namespace EduPlatform.Modules.Identity.Application.Authentication;

/// <summary>
/// Ends the session the presented refresh token belongs to.
/// </summary>
/// <remarks>
/// Succeeds even when the token is unknown or already spent. Sign-out is not a place to tell a
/// caller whether a token was real, and a client clearing its cookie should never see an error.
/// </remarks>
public sealed record SignOutCommand(string? RefreshToken) : ICommand;

internal sealed class SignOutHandler(
    IUserRepository users,
    ITokenHasher tokenHasher,
    IAuditLog auditLog,
    ICurrentUser currentUser) : CommandHandler<SignOutCommand>
{
    protected override async Task HandleCoreAsync(SignOutCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return;
        }

        var user = await users
            .FindByRefreshTokenHashAsync(tokenHasher.Hash(command.RefreshToken), cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return;
        }

        user.RevokeAllTokens("signed out");
        auditLog.Record(AuditEntry.Record(
            AuditActions.SignedOut,
            actorUserId: user.Id,
            subjectUserId: user.Id,
            ipAddress: currentUser.IpAddress));

        await users.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
