using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Identity.Application.Abstractions;
using EduPlatform.Modules.Identity.Contracts;
using EduPlatform.Modules.Identity.Domain;
using FluentValidation;

namespace EduPlatform.Modules.Identity.Application.Users;

/// <summary>
/// Creates an account on an administrator's behalf, in any role.
/// </summary>
/// <remarks>
/// This is how teachers and administrators come into being — the public registration form
/// refuses those roles. The address is marked confirmed straight away, because an administrator
/// entering a colleague's address is the confirmation.
/// </remarks>
public sealed record CreateUserCommand(
    string Email,
    string FirstName,
    string? MiddleName,
    string LastName,
    string Password,
    UserRole Role) : ICommand<UserDto>;

internal sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(command => command.Email).NotEmpty().MaximumLength(Email.MaxLength);
        RuleFor(command => command.FirstName).NotEmpty().MaximumLength(PersonName.MaxPartLength);
        RuleFor(command => command.MiddleName).MaximumLength(PersonName.MaxPartLength);
        RuleFor(command => command.LastName).NotEmpty().MaximumLength(PersonName.MaxPartLength);
        RuleFor(command => command.Role).IsInEnum();

        RuleFor(command => command.Password)
            .NotEmpty()
            .MinimumLength(PasswordPolicy.MinimumLength)
            .WithMessage($"Password must be at least {PasswordPolicy.MinimumLength} characters long.")
            .MaximumLength(PasswordPolicy.MaximumLength)
            .Must(PasswordPolicy.IsStrongEnough)
            .WithMessage("Password must contain a letter and a digit.");
    }
}

internal sealed class CreateUserHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IAuditLog auditLog,
    ICurrentUser currentUser) : ICommandHandler<CreateUserCommand, UserDto>
{
    public async Task<UserDto> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var email = Email.Create(command.Email);

        if (await users.EmailExistsAsync(email, cancellationToken).ConfigureAwait(false))
        {
            throw new DomainException("Вече има акаунт с този адрес.");
        }

        var user = User.Register(
            email,
            PersonName.Create(command.FirstName, command.MiddleName, command.LastName),
            passwordHasher.Hash(command.Password),
            command.Role);

        user.ConfirmEmail();
        users.Add(user);

        auditLog.Record(AuditEntry.Record(
            AuditActions.UserRegistered,
            actorUserId: currentUser.UserId,
            subjectUserId: user.Id,
            detail: $"created by administrator, role={command.Role}",
            ipAddress: currentUser.IpAddress));

        await users.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return user.ToDto();
    }
}
