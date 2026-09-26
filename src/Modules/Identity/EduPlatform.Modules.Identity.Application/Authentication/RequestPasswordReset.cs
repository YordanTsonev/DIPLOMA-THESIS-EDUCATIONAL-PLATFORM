using EduPlatform.BuildingBlocks.Application.Abstractions;
using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Identity.Application.Abstractions;
using EduPlatform.Modules.Identity.Domain;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace EduPlatform.Modules.Identity.Application.Authentication;

/// <summary>
/// Mails a reset link, if the address belongs to an account.
/// </summary>
/// <remarks>
/// Always reports success. Answering differently for a known and an unknown address would turn
/// this endpoint into a way to enumerate which people have accounts — the opposite of the
/// registration form, where telling the truth is unavoidable.
/// </remarks>
public sealed record RequestPasswordResetCommand(string Email) : ICommand;

internal sealed class RequestPasswordResetValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetValidator() => RuleFor(command => command.Email).NotEmpty();
}

internal sealed class RequestPasswordResetHandler(
    IUserRepository users,
    ITokenHasher tokenHasher,
    IEmailSender emailSender,
    IAuditLog auditLog,
    ICurrentUser currentUser,
    IClock clock,
    IOptions<IdentitySettings> settings) : CommandHandler<RequestPasswordResetCommand>
{
    protected override async Task HandleCoreAsync(
        RequestPasswordResetCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        User? user;
        try
        {
            user = await users.FindByEmailAsync(Email.Create(command.Email), cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException)
        {
            return;
        }

        if (user is null || !user.IsActive)
        {
            return;
        }

        var lifetime = settings.Value.PasswordResetLifetime;
        var (token, hash) = tokenHasher.Generate();
        user.IssueSecurityToken(UserTokenPurpose.PasswordReset, hash, clock.UtcNow.Add(lifetime));

        auditLog.Record(AuditEntry.Record(
            AuditActions.PasswordResetRequested,
            subjectUserId: user.Id,
            ipAddress: currentUser.IpAddress));

        await users.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await emailSender.SendAsync(
            user.Email.Value,
            "Нова парола за EduPlatform",
            EmailTemplates.ResetPassword(user.Name.First, settings.Value.ClientBaseUrl, token, lifetime),
            cancellationToken).ConfigureAwait(false);
    }
}
