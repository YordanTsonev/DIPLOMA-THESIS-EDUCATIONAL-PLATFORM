using EduPlatform.Modules.Identity.Domain;
using Microsoft.AspNetCore.Authorization;

namespace EduPlatform.Api.Authorization;

/// <summary>
/// Named policies, so endpoints reference a rule rather than repeat a role string.
/// </summary>
/// <remarks>
/// These are the coarse gate only. From Phase 2 a teacher may reach a class they teach and no
/// other, which no role check can express — that is resource-based authorisation, added
/// alongside these policies rather than in place of them.
/// </remarks>
internal static class AuthorizationPolicies
{
    public const string AdminOnly = "admin-only";
    public const string TeacherOrAdmin = "teacher-or-admin";
    public const string StudentOnly = "student-only";
    public const string ParentOnly = "parent-only";

    public static AuthorizationOptions AddEduPlatformPolicies(this AuthorizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.AddPolicy(AdminOnly, policy => policy.RequireRole(nameof(UserRole.Admin)));

        options.AddPolicy(TeacherOrAdmin, policy => policy
            .RequireRole(nameof(UserRole.Teacher), nameof(UserRole.Admin)));

        options.AddPolicy(StudentOnly, policy => policy.RequireRole(nameof(UserRole.Student)));
        options.AddPolicy(ParentOnly, policy => policy.RequireRole(nameof(UserRole.Parent)));

        // Anything not explicitly marked AllowAnonymous requires a signed-in user. Forgetting
        // an attribute then fails closed rather than exposing an endpoint.
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        return options;
    }
}
