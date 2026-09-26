using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Identity.Application.Abstractions;
using EduPlatform.Modules.Identity.Contracts;

namespace EduPlatform.Modules.Identity.Application.Users;

/// <summary>Returns the signed-in user, so the client can restore its state after a reload.</summary>
public sealed record GetCurrentUserQuery : IQuery<UserDto>;

internal sealed class GetCurrentUserHandler(IUserReadStore users, ICurrentUser currentUser)
    : IQueryHandler<GetCurrentUserQuery, UserDto>
{
    public async Task<UserDto> HandleAsync(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new DomainException("Няма влязъл потребител.");

        return await users.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Потребителят вече не съществува.");
    }
}
