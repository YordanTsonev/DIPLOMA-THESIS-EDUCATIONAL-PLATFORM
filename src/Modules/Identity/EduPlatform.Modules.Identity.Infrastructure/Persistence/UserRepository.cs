using EduPlatform.Modules.Identity.Application.Abstractions;
using EduPlatform.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// Loads and saves the <see cref="User"/> aggregate.
/// </summary>
/// <remarks>
/// Every lookup includes the token collections, because the domain methods that rotate and
/// revoke them operate on the whole set — a partially loaded aggregate would silently skip
/// tokens it never saw.
/// </remarks>
internal sealed class UserRepository(IdentityDbContext dbContext) : IUserRepository
{
    public Task<User?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        WithTokens().SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);

    public Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        WithTokens().SingleOrDefaultAsync(user => user.EmailAddress == email.Value, cancellationToken);

    public Task<User?> FindByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        WithTokens().SingleOrDefaultAsync(
            user => user.RefreshTokens.Any(token => token.TokenHash == tokenHash),
            cancellationToken);

    public Task<User?> FindBySecurityTokenHashAsync(
        UserTokenPurpose purpose,
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        WithTokens().SingleOrDefaultAsync(
            user => user.SecurityTokens.Any(token => token.Purpose == purpose && token.TokenHash == tokenHash),
            cancellationToken);

    public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default) =>
        dbContext.Users.AnyAsync(user => user.EmailAddress == email.Value, cancellationToken);

    public void Add(User user) => dbContext.Users.Add(user);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<User> WithTokens() =>
        dbContext.Users
            .Include(user => user.RefreshTokens)
            .Include(user => user.SecurityTokens);
}
