using EduPlatform.BuildingBlocks.Application.Abstractions;
using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Identity.Application.Abstractions;
using EduPlatform.Modules.Identity.Contracts;
using EduPlatform.Modules.Identity.Domain;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace EduPlatform.Modules.Identity.Application.Authentication;

/// <summary>
/// Creates an account and mails a confirmation link.
/// </summary>
/// <remarks>
/// Only <see cref="UserRole.Student"/> and <see cref="UserRole.Parent"/> may be chosen here.
/// Teacher and Admin accounts carry authority over other people's data and are created by an
/// administrator, never by whoever fills in the public form.
/// </remarks>
public sealed record RegisterUserCommand(
    string Email,
    string FirstName,
    string? MiddleName,
    string LastName,
    string Password,
    UserRole Role) : ICommand<UserDto>;

internal sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    private static readonly UserRole[] SelfServiceRoles = [UserRole.Student, UserRole.Parent];

    public RegisterUserValidator()
    {
        RuleFor(command => command.Email).NotEmpty().MaximumLength(Email.MaxLength);
        RuleFor(command => command.FirstName).NotEmpty().MaximumLength(PersonName.MaxPartLength);
        RuleFor(command => command.MiddleName).MaximumLength(PersonName.MaxPartLength);
        RuleFor(command => command.LastName).NotEmpty().MaximumLength(PersonName.MaxPartLength);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MinimumLength(PasswordPolicy.MinimumLength)
            .WithMessage($"Password must be at least {PasswordPolicy.MinimumLength} characters long.")
            .MaximumLength(PasswordPolicy.MaximumLength)
            .Must(PasswordPolicy.IsStrongEnough)
            .WithMessage("Password must contain a letter and a digit.");

        RuleFor(command => command.Role)
            .Must(role => SelfServiceRoles.Contains(role))
            .WithMessage("Only student and parent accounts can be created through registration.");
    }
}

internal sealed class RegisterUserHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenHasher tokenHasher,
    IEmailSender emailSender,
    IAuditLog auditLog,
    ICurrentUser currentUser,
    IClock clock,
    IOptions<IdentitySettings> settings) : ICommandHandler<RegisterUserCommand, UserDto>
{
    public async Task<UserDto> HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var email = Email.Create(command.Email);

        if (await users.EmailExistsAsync(email, cancellationToken).ConfigureAwait(false))
        {
            // Registration is the one flow where revealing that an address is taken is
            // unavoidable — the alternative silently loses accounts. Password reset does the
            // opposite, and says nothing.
            throw new DomainException("An account with this e-mail address already exists.");
        }

        var user = User.Register(
            email,
            PersonName.Create(command.FirstName, command.MiddleName, command.LastName),
            passwordHasher.Hash(command.Password),
            command.Role);

        var (token, hash) = tokenHasher.Generate();
        user.IssueSecurityToken(
            UserTokenPurpose.EmailConfirmation,
            hash,
            clock.UtcNow.Add(settings.Value.EmailConfirmationLifetime));

        users.Add(user);
        auditLog.Record(AuditEntry.Record(
            AuditActions.UserRegistered,
            subjectUserId: user.Id,
            detail: $"role={command.Role}",
            ipAddress: currentUser.IpAddress));

        await users.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await emailSender.SendAsync(
            email.Value,
            "Потвърдете своята регистрация",
            EmailTemplates.ConfirmEmail(user.Name.First, settings.Value.ClientBaseUrl, token),
            cancellationToken).ConfigureAwait(false);

        return user.ToDto();
    }
}
