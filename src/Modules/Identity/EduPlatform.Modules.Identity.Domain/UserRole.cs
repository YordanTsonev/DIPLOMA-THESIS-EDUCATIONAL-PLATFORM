namespace EduPlatform.Modules.Identity.Domain;

/// <summary>
/// The four roles the platform recognises. A user holds exactly one.
/// </summary>
/// <remarks>
/// Modelled as an enum rather than a table because the set is fixed by the domain: adding a role
/// always means writing code (new screens, new permissions), never just inserting a row. Explicit
/// values keep the stored numbers stable if the list is ever reordered.
/// </remarks>
public enum UserRole
{
    /// <summary>Sees their own timetable, materials, homework, tests and grades.</summary>
    Student = 1,

    /// <summary>Teaches specific subjects to specific classes; acts only within those assignments.</summary>
    Teacher = 2,

    /// <summary>Sees the data of their own children only.</summary>
    Parent = 3,

    /// <summary>Manages users and the school structure.</summary>
    Admin = 4,
}
