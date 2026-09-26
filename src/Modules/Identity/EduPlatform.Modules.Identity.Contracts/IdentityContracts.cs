namespace EduPlatform.Modules.Identity.Contracts;

/// <summary>
/// The shape of a user as other modules and the client see it. Deliberately excludes the
/// password hash and the token collections — nothing outside this module needs them.
/// </summary>
public sealed record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string? MiddleName,
    string LastName,
    string FullName,
    string Role,
    bool IsActive,
    bool EmailConfirmed,
    string? PhoneNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSignedInAt);

/// <summary>A page of users, for the administration screen.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>
/// The result of a successful sign-in or refresh. The refresh token is returned separately so
/// the API can place it in a cookie the browser's JavaScript cannot read.
/// </summary>
public sealed record AuthenticationResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserDto User);

/// <summary>
/// Read-only view of the Identity module for other modules. SchoolStructure uses it from
/// Phase 2 to resolve the person behind a student or teacher record.
/// </summary>
public interface IUserDirectory
{
    Task<UserDto?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDto>> FindByIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default);
}
