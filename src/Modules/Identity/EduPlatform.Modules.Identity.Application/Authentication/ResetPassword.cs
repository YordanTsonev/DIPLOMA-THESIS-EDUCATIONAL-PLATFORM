using EduPlatform.BuildingBlocks.Application.Abstractions;
using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Identity.Application.Abstractions;
using EduPlatform.Modules.Identity.Domain;
using FluentValidation;

namespace EduPlatform.Modules.Identity.Application.Authentication;

public sealed record ResetPasswordCommand(string Token, string NewPassword) : ICommand;

internal sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(command => command.Token).NotEmpty();

        RuleFor(command => command.NewPassword)
            .NotEmpty()
            .MinimumLength(PasswordPolicy.MinimumLength)
            .WithMessage($"Password must be at least {PasswordPolicy.MinimumLength} characters long.")
            .MaximumLength(PasswordPolicy.MaximumLength)
            .Must(PasswordPolicy.IsStrongEnough)
            .WithMessage("Password must contain a letter and a digit.");
    }
}

internal sealed class ResetPasswordHandler(
    IUserRepository users,
    ITokenHasher tokenHasher,
    IPasswordHasher passwordHasher,
    IAuditLog auditLog,
    ICurrentUser currentUser,
    IClock clock) : CommandHandler<ResetPasswordCommand>
{
    protected override async Task HandleCoreAsync(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var hash = tokenHasher.Hash(command.Token);

        var user = await users
            .FindBySecurityTokenHashAsync(UserTokenPurpose.PasswordReset, hash, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new DomainException("Тази връзка не е валидна.");

        user.ConsumeSecurityToken(UserTokenPurpose.PasswordReset, hash, clock.UtcNow);

        // Also confirms the address: the link could only have been read from that mailbox.
        // Revokes every session too, which is what locks out whoever prompted the reset.
        user.ResetPassword(passwordHasher.Hash(command.NewPassword));

        auditLog.Record(AuditEntry.Record(
            AuditActions.PasswordReset,
            actorUserId: user.Id,
            subjectUserId: user.Id,
            ipAddress: currentUser.IpAddress));

        await users.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
