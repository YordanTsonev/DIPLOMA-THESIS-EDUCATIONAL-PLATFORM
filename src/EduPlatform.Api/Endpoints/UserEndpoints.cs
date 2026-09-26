using EduPlatform.Api.Authorization;
using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.Modules.Identity.Application.Users;
using EduPlatform.Modules.Identity.Contracts;
using EduPlatform.Modules.Identity.Domain;

namespace EduPlatform.Api.Endpoints;

/// <summary>
/// User administration. Every endpoint here is restricted to administrators — these operations
/// grant and remove authority over other people's data.
/// </summary>
internal static class UserEndpoints
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/users")
            .WithTags("Users")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        // Paging parameters are nullable so they can be omitted: a non-nullable int bound from
        // the query string is *required* by minimal APIs, which would make a plain
        // "GET /api/v1/users" fail rather than return the first page.
        group.MapGet("/", async (
                string? search,
                UserRole? role,
                bool? isActive,
                int? page,
                int? pageSize,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var result = await dispatcher.QueryAsync(
                    new ListUsersQuery(
                        search,
                        role,
                        isActive,
                        page is null or <= 0 ? 1 : page.Value,
                        pageSize is null or <= 0 ? DefaultPageSize : pageSize.Value),
                    cancellationToken);

                return Results.Ok(result);
            })
            .WithName("ListUsers")
            .WithSummary("Paged and filterable list of accounts.")
            .Produces<PagedResult<UserDto>>();

        group.MapPost("/", async (
                CreateUserRequest request,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var user = await dispatcher.SendAsync(
                    new CreateUserCommand(
                        request.Email,
                        request.FirstName,
                        request.MiddleName,
                        request.LastName,
                        request.Password,
                        request.Role),
                    cancellationToken);

                return Results.Created($"/api/v1/users/{user.Id}", user);
            })
            .WithName("CreateUser")
            .WithSummary("Creates an account in any role. This is how teachers and admins are made.")
            .Produces<UserDto>(StatusCodes.Status201Created);

        group.MapPut("/{id:guid}/role", async (
                Guid id,
                ChangeRoleRequest request,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var user = await dispatcher.SendAsync(new ChangeUserRoleCommand(id, request.Role), cancellationToken);
                return Results.Ok(user);
            })
            .WithName("ChangeUserRole")
            .WithSummary("Changes a role and revokes the user's sessions, so the old token cannot outlive it.")
            .Produces<UserDto>();

        group.MapPut("/{id:guid}/active", async (
                Guid id,
                SetActiveRequest request,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var user = await dispatcher.SendAsync(new SetUserActiveCommand(id, request.IsActive), cancellationToken);
                return Results.Ok(user);
            })
            .WithName("SetUserActive")
            .WithSummary("Deactivates or restores an account. Accounts are never deleted.")
            .Produces<UserDto>();

        return endpoints;
    }
}

internal sealed record CreateUserRequest(
    string Email,
    string FirstName,
    string? MiddleName,
    string LastName,
    string Password,
    UserRole Role);

internal sealed record ChangeRoleRequest(UserRole Role);

internal sealed record SetActiveRequest(bool IsActive);
