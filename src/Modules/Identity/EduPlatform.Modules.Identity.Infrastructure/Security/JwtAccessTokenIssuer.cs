using System.Globalization;
using System.Security.Claims;
using System.Text;
using EduPlatform.BuildingBlocks.Application.Abstractions;
using EduPlatform.Modules.Identity.Application.Abstractions;
using EduPlatform.Modules.Identity.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EduPlatform.Modules.Identity.Infrastructure.Security;

/// <summary>Signs the short-lived JWT the client attaches to every request.</summary>
internal sealed class JwtAccessTokenIssuer(IOptions<JwtOptions> options, IClock clock) : IAccessTokenIssuer
{
    private readonly JsonWebTokenHandler _handler = new();

    public (string Token, DateTimeOffset ExpiresAt) Issue(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var settings = options.Value;
        var issuedAt = clock.UtcNow;
        var expiresAt = issuedAt.Add(settings.AccessTokenLifetime);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = user.Email.Value,
                [JwtRegisteredClaimNames.Jti] = Guid.CreateVersion7().ToString(),
                [ClaimTypes.Role] = user.Role.ToString(),
                ["name"] = user.Name.Full,

                // Lets the client render the right navigation without a second request.
                ["role_id"] = ((int)user.Role).ToString(CultureInfo.InvariantCulture),
            },
        };

        return (_handler.CreateToken(descriptor), expiresAt);
    }
}
