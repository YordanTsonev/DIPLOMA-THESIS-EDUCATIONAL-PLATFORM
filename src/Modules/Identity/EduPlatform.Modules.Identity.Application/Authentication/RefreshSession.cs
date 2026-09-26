using EduPlatform.BuildingBlocks.Application.Abstractions;
using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Identity.Application.Abstractions;
using EduPlatform.Modules.Identity.Contracts;
using EduPlatform.Modules.Identity.Domain;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace EduPlatform.Modules.Identity.Application.Authentication;

/// <summary>Exchanges a refresh token for a fresh access token and a new refresh token.</summary>
public sealed record RefreshSessionCommand(string RefreshToken, string? DeviceInfo = null)
    : ICommand<AuthenticationResult>;

internal sealed class RefreshSessionValidator : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionValidator() => RuleFor(command => command.RefreshToken).NotEmpty();
}

internal sealed class RefreshSessionHandler(
    IUserRepository users,
    ITokenHasher tokenHasher,
    IAccessTokenIssuer accessTokens,
    IAuditLog auditLog,
    ICurrentUser currentUser,
    IClock clock,
    IOptions<IdentitySettings> settings) : ICommandHandler<RefreshSessionCommand, AuthenticationResult>
{
    private const string Rejected = "Сесията е изтекла. Влезте отново.";

    public async Task<AuthenticationResult> HandleAsync(RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var presentedHash = tokenHasher.Hash(command.RefreshToken);

        var user = await users.FindByRefreshTokenHashAsync(presentedHash, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException(Rejected);

        if (!user.IsActive)
        {
            throw new DomainException(Rejected);
        }

        var (newToken, newTokenHash) = tokenHasher.Generate();
        var expiresAt = clock.UtcNow.Add(settings.Value.RefreshTokenLifetime);

        try
        {
            user.RotateRefreshToken(presentedHash, newTokenHash, expiresAt, clock.UtcNow, command.DeviceInfo);
        }
        catch (DomainException)
        {
            // The aggregate has already revoked every session at this point. Persist that,
            // record why, and give the client the same generic answer as any other failure.
            auditLog.Record(AuditEntry.Record(
                AuditActions.RefreshTokenReplayDetected,
                subjectUserId: user.Id,
                detail: "presented an already-used or expired refresh token",
                ipAddress: currentUser.IpAddress));

            await users.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            throw new DomainException(Rejected);
        }

        var (accessToken, accessExpiresAt) = accessTokens.Issue(user);

        auditLog.Record(AuditEntry.Record(
            AuditActions.SessionRefreshed,
            actorUserId: user.Id,
            subjectUserId: user.Id,
            ipAddress: currentUser.IpAddress));

        await users.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AuthenticationResult(accessToken, accessExpiresAt, newToken, expiresAt, user.ToDto());
    }
}
