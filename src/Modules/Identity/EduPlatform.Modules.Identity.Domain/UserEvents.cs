using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.Identity.Domain;

/// <summary>A new account was created. SchoolStructure listens from Phase 2 to create the matching profile.</summary>
public sealed record UserRegistered(Guid UserId, string Email, UserRole Role) : DomainEvent;

/// <summary>The account confirmed its e-mail address and may now sign in.</summary>
public sealed record UserEmailConfirmed(Guid UserId, string Email) : DomainEvent;

/// <summary>An administrator changed a user's role. Always audited.</summary>
public sealed record UserRoleChanged(Guid UserId, UserRole PreviousRole, UserRole NewRole) : DomainEvent;

/// <summary>The account was deactivated. Active sessions are revoked in response.</summary>
public sealed record UserDeactivated(Guid UserId) : DomainEvent;

/// <summary>The account was reactivated by an administrator.</summary>
public sealed record UserReactivated(Guid UserId) : DomainEvent;
