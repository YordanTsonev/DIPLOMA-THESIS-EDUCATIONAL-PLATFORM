using EduPlatform.Modules.Identity.Application;
using EduPlatform.Modules.Identity.Application.Abstractions;
using EduPlatform.Modules.Identity.Contracts;
using EduPlatform.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// Read-only projections for listings and for other modules.
/// </summary>
/// <remarks>
/// Queries are tracked nowhere and never load the token collections, unlike
/// <see cref="UserRepository"/>. Nothing on a listing screen needs an aggregate.
/// </remarks>
internal sealed class UserReadStore(IdentityDbContext dbContext) : IUserReadStore, IUserDirectory
{
    public async Task<PagedResult<UserDto>> ListAsync(
        string? search,
        UserRole? role,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";

            // ILIKE rather than ToLower(): it is case-insensitive in the database and handles
            // Bulgarian casing correctly, which an invariant lower-casing in C# would not.
            query = query.Where(user =>
                EF.Functions.ILike(user.EmailAddress, pattern)
                || EF.Functions.ILike(user.Name.First, pattern)
                || EF.Functions.ILike(user.Name.Last, pattern));
        }

        if (role is not null)
        {
            query = query.Where(user => user.Role == role);
        }

        if (isActive is not null)
        {
            query = query.Where(user => user.IsActive == isActive);
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var users = await query
            .OrderBy(user => user.Name.Last)
            .ThenBy(user => user.Name.First)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<UserDto>(
            users.Select(user => user.ToDto()).ToList(),
            page,
            pageSize,
            totalCount);
    }

    public async Task<UserDto?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        return user?.ToDto();
    }

    public async Task<IReadOnlyList<UserDto>> FindByIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        if (userIds.Count == 0)
        {
            return [];
        }

        var users = await dbContext.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return users.Select(user => user.ToDto()).ToList();
    }
}
