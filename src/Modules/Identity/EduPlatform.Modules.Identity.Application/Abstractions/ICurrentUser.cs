using EduPlatform.Modules.Identity.Domain;

namespace EduPlatform.Modules.Identity.Application.Abstractions;

/// <summary>Who is making the current request, read from the validated access token.</summary>
public interface ICurrentUser
{
    /// <summary>Null when the request is anonymous.</summary>
    Guid? UserId { get; }

    UserRole? Role { get; }

    string? IpAddress { get; }

    bool IsAuthenticated => UserId is not null;
}
