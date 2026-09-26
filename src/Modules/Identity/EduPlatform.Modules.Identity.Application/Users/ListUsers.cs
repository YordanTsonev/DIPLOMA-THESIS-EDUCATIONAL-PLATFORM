using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.Modules.Identity.Application.Abstractions;
using EduPlatform.Modules.Identity.Contracts;
using EduPlatform.Modules.Identity.Domain;
using FluentValidation;

namespace EduPlatform.Modules.Identity.Application.Users;

/// <summary>Paged, filterable list for the administration screen.</summary>
public sealed record ListUsersQuery(
    string? Search = null,
    UserRole? Role = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<UserDto>>;

internal sealed class ListUsersValidator : AbstractValidator<ListUsersQuery>
{
    public ListUsersValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);

        // An unbounded page size lets one request pull the whole table.
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}

internal sealed class ListUsersHandler(IUserReadStore users)
    : IQueryHandler<ListUsersQuery, PagedResult<UserDto>>
{
    public Task<PagedResult<UserDto>> HandleAsync(ListUsersQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return users.ListAsync(
            query.Search,
            query.Role,
            query.IsActive,
            query.Page,
            query.PageSize,
            cancellationToken);
    }
}
