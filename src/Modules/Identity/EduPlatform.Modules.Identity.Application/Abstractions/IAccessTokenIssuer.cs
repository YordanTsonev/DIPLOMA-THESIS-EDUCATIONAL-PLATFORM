using EduPlatform.Modules.Identity.Domain;

namespace EduPlatform.Modules.Identity.Application.Abstractions;

/// <summary>Issues the short-lived JWT the client sends on every request.</summary>
public interface IAccessTokenIssuer
{
    (string Token, DateTimeOffset ExpiresAt) Issue(User user);
}
