using System.Globalization;
using System.Security.Claims;
using EduPlatform.Modules.Identity.Application.Abstractions;
using EduPlatform.Modules.Identity.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;

namespace EduPlatform.Modules.Identity.Infrastructure.Security;

/// <summary>
/// Reads the caller's identity from the validated access token.
/// </summary>
/// <remarks>
/// Every value comes from claims the JWT middleware has already verified, so nothing here
/// trusts a header the client could have written itself.
/// </remarks>
internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId =>
        Guid.TryParse(FindClaim(JwtRegisteredClaimNames.Sub) ?? FindClaim(ClaimTypes.NameIdentifier), out var id)
            ? id
            : null;

    public UserRole? Role =>
        int.TryParse(FindClaim("role_id"), CultureInfo.InvariantCulture, out var value) && Enum.IsDefined((UserRole)value)
            ? (UserRole)value
            : null;

    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private string? FindClaim(string type) => accessor.HttpContext?.User.FindFirst(type)?.Value;
}
