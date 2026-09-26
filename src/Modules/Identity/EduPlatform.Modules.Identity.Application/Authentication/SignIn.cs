using EduPlatform.BuildingBlocks.Application.Abstractions;
using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Identity.Application.Abstractions;
using EduPlatform.Modules.Identity.Contracts;
using EduPlatform.Modules.Identity.Domain;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace EduPlatform.Modules.Identity.Application.Authentication;

public sealed record SignInCommand(string Email, string Password, string? DeviceInfo = null)
    : ICommand<AuthenticationResult>;

internal sealed class SignInValidator : AbstractValidator<SignInCommand>
{
    public SignInValidator()
    {
        RuleFor(command => command.Email).NotEmpty();
        RuleFor(command => command.Password).NotEmpty();
    }
}

internal sealed class SignInHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenHasher tokenHasher,
    IAccessTokenIssuer accessTokens,
    IAuditLog auditLog,
    ICurrentUser currentUser,
    IClock clock,
    IOptions<IdentitySettings> settings) : ICommandHandler<SignInCommand, AuthenticationResult>
{
    /// <summary>
    /// One message for every failure mode. Distinguishing "no such account" from "wrong password"
    /// turns the sign-in form into a tool for discovering which addresses are registered.
    /// </summary>
    private const string SignInFailed = "Невалиден адрес или парола.";

    public async Task<AuthenticationResult> HandleAsync(SignInCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        User? user = null;
        try
        {
            user = await users.FindByEmailAsync(Email.Create(command.Email), cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException)
        {
            // A malformed address simply cannot match an account; it is a failed attempt, not a
            // validation error to explain back to whoever is probing the form.
        }

        if (user is null || passwordHasher.Verify(user.PasswordHash, command.Password) == PasswordVerificationOutcome.Failed)
        {
            await RecordFailureAsync(user?.Id, "bad credentials", cancellationToken).ConfigureAwait(false);
            throw new DomainException(SignInFailed);
        }

        if (!user.IsActive)
        {
            await RecordFailureAsync(user.Id, "account deactivated", cancellationToken).ConfigureAwait(false);
            throw new DomainException("Този акаунт е деактивиран. Обърнете се към администратор.");
        }

        if (!user.EmailConfirmed)
        {
            await RecordFailureAsync(user.Id, "email not confirmed", cancellationToken).ConfigureAwait(false);
            throw new DomainException("Потвърдете адреса си по изпратената връзка, преди да влезете.");
        }

        var (refreshToken, refreshTokenHash) = tokenHasher.Generate();
        var refreshExpiresAt = clock.UtcNow.Add(settings.Value.RefreshTokenLifetime);
        user.IssueRefreshToken(refreshTokenHash, refreshExpiresAt, command.DeviceInfo);
        user.RecordSignIn();

        var (accessToken, accessExpiresAt) = accessTokens.Issue(user);

        auditLog.Record(AuditEntry.Record(
            AuditActions.SignInSucceeded,
            actorUserId: user.Id,
            subjectUserId: user.Id,
            ipAddress: currentUser.IpAddress));

        await users.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AuthenticationResult(accessToken, accessExpiresAt, refreshToken, refreshExpiresAt, user.ToDto());
    }

    private async Task RecordFailureAsync(Guid? userId, string reason, CancellationToken cancellationToken)
    {
        // Persisted even though the request fails: a run of failures against one account is
        // exactly the pattern worth seeing afterwards.
        auditLog.Record(AuditEntry.Record(
            AuditActions.SignInFailed,
            subjectUserId: userId,
            detail: reason,
            ipAddress: currentUser.IpAddress));

        await users.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
