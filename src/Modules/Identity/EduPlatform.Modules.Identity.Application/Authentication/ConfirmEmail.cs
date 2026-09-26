using EduPlatform.BuildingBlocks.Application.Abstractions;
using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Identity.Application.Abstractions;
using EduPlatform.Modules.Identity.Domain;
using FluentValidation;

namespace EduPlatform.Modules.Identity.Application.Authentication;

public sealed record ConfirmEmailCommand(string Token) : ICommand;

internal sealed class ConfirmEmailValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailValidator() => RuleFor(command => command.Token).NotEmpty();
}

internal sealed class ConfirmEmailHandler(
    IUserRepository users,
    ITokenHasher tokenHasher,
    IAuditLog auditLog,
    ICurrentUser currentUser,
    IClock clock) : CommandHandler<ConfirmEmailCommand>
{
    protected override async Task HandleCoreAsync(ConfirmEmailCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var hash = tokenHasher.Hash(command.Token);

        var user = await users
            .FindBySecurityTokenHashAsync(UserTokenPurpose.EmailConfirmation, hash, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new DomainException("Тази връзка не е валидна.");

        // Confirming twice is a normal thing for a person to do — they click the mail again.
        // Treat it as success rather than as an error to explain.
        if (user.EmailConfirmed)
        {
            return;
        }

        user.ConsumeSecurityToken(UserTokenPurpose.EmailConfirmation, hash, clock.UtcNow);
        user.ConfirmEmail();

        auditLog.Record(AuditEntry.Record(
            AuditActions.EmailConfirmed,
            actorUserId: user.Id,
            subjectUserId: user.Id,
            ipAddress: currentUser.IpAddress));

        await users.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
