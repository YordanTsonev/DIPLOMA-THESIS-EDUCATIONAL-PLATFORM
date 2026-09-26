using EduPlatform.Modules.Identity.Contracts;
using EduPlatform.Modules.Identity.Domain;

namespace EduPlatform.Modules.Identity.Application.Abstractions;

/// <summary>
/// Read-side access for listings.
/// </summary>
/// <remarks>
/// Separate from <see cref="IUserRepository"/> because the two have different jobs: the
/// repository loads whole aggregates so the domain can enforce rules, while this projects
/// straight to DTOs. Loading 50 users with their token collections to render a table would be
/// wasteful, and nothing on that screen needs the aggregate.
/// </remarks>
public interface IUserReadStore
{
    Task<PagedResult<UserDto>> ListAsync(
        string? search,
        UserRole? role,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<UserDto?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDto>> FindByIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default);
}
