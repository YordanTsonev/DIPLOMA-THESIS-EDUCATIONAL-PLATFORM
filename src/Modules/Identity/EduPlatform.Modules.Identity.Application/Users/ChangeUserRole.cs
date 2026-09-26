using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Identity.Application.Abstractions;
using EduPlatform.Modules.Identity.Contracts;
using EduPlatform.Modules.Identity.Domain;
using FluentValidation;

namespace EduPlatform.Modules.Identity.Application.Users;

public sealed record ChangeUserRoleCommand(Guid UserId, UserRole Role) : ICommand<UserDto>;

internal sealed class ChangeUserRoleValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Role).IsInEnum();
    }
}

internal sealed class ChangeUserRoleHandler(
    IUserRepository users,
    IAuditLog auditLog,
    ICurrentUser currentUser) : ICommandHandler<ChangeUserRoleCommand, UserDto>
{
    public async Task<UserDto> HandleAsync(ChangeUserRoleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await users.FindByIdAsync(command.UserId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Потребителят не е намерен.");

        // An administrator who demotes themselves by mistake locks everyone out of user
        // management, and nothing in the product can undo it.
        if (user.Id == currentUser.UserId && command.Role != user.Role)
        {
            throw new DomainException("Не можете да промените собствената си роля.");
        }

        var previousRole = user.Role;
        user.ChangeRole(command.Role);

        if (previousRole != command.Role)
        {
            // The new role decides what the access token allows, so the old one must not outlive it.
            user.RevokeAllTokens("role changed");

            auditLog.Record(AuditEntry.Record(
                AuditActions.RoleChanged,
                actorUserId: currentUser.UserId,
                subjectUserId: user.Id,
                detail: $"{previousRole} -> {command.Role}",
                ipAddress: currentUser.IpAddress));
        }

        await users.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return user.ToDto();
    }
}
