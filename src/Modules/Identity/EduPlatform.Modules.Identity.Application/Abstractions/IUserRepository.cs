using EduPlatform.Modules.Identity.Domain;

namespace EduPlatform.Modules.Identity.Application.Abstractions;

/// <summary>
/// Persistence for the <see cref="User"/> aggregate.
/// </summary>
/// <remarks>
/// An interface rather than a DbContext injected directly, because the Application layer must
/// not reference EF Core — a rule the architecture tests enforce. Each lookup returns the whole
/// aggregate with the collections the caller needs, so the domain can enforce its own rules.
/// </remarks>
public interface IUserRepository
{
    Task<User?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>Loads the user owning a refresh token, by the token's hash.</summary>
    Task<User?> FindByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Loads the user owning an e-mailed link, by the token's hash.</summary>
    Task<User?> FindBySecurityTokenHashAsync(
        UserTokenPurpose purpose,
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default);

    void Add(User user);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
