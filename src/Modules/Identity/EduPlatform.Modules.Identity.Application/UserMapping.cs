using EduPlatform.Modules.Identity.Contracts;
using EduPlatform.Modules.Identity.Domain;

namespace EduPlatform.Modules.Identity.Application;

/// <summary>
/// Projects the aggregate onto the contract other modules and the client consume.
/// Kept in one place so no caller can accidentally expose the password hash.
/// </summary>
public static class UserMapping
{
    public static UserDto ToDto(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserDto(
            user.Id,
            user.Email.Value,
            user.Name.First,
            user.Name.Middle,
            user.Name.Last,
            user.Name.Full,
            user.Role.ToString(),
            user.IsActive,
            user.EmailConfirmed,
            user.PhoneNumber,
            user.CreatedAt,
            user.LastSignedInAt);
    }
}
