using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Identity.Application.Abstractions;
using EduPlatform.Modules.Identity.Contracts;
using EduPlatform.Modules.Identity.Domain;
using FluentValidation;

namespace EduPlatform.Modules.Identity.Application.Users;

/// <summary>
/// Deactivates or restores an account. Deactivation is used instead of deletion: grades,
/// submissions and audit entries reference the user, and removing the row would orphan them.
/// </summary>
public sealed record SetUserActiveCommand(Guid UserId, bool IsActive) : ICommand<UserDto>;

internal sealed class SetUserActiveValidator : AbstractValidator<SetUserActiveCommand>
{
    public SetUserActiveValidator() => RuleFor(command => command.UserId).NotEmpty();
}

internal sealed class SetUserActiveHandler(
    IUserRepository users,
    IAuditLog auditLog,
    ICurrentUser currentUser) : ICommandHandler<SetUserActiveCommand, UserDto>
{
    public async Task<UserDto> HandleAsync(SetUserActiveCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await users.FindByIdAsync(command.UserId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Потребителят не е намерен.");

        if (user.Id == currentUser.UserId && !command.IsActive)
        {
            throw new DomainException("Не можете да деактивирате собствения си акаунт.");
        }

        var wasActive = user.IsActive;

        if (command.IsActive)
        {
            user.Reactivate();
        }
        else
        {
            user.Deactivate();
        }

        if (wasActive != command.IsActive)
        {
            auditLog.Record(AuditEntry.Record(
                command.IsActive ? AuditActions.Reactivated : AuditActions.Deactivated,
                actorUserId: currentUser.UserId,
                subjectUserId: user.Id,
                ipAddress: currentUser.IpAddress));
        }

        await users.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return user.ToDto();
    }
}
