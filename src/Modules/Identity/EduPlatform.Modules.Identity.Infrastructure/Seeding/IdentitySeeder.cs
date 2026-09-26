using EduPlatform.BuildingBlocks.Infrastructure.Seeding;
using EduPlatform.Modules.Identity.Application.Abstractions;
using EduPlatform.Modules.Identity.Domain;
using EduPlatform.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Modules.Identity.Infrastructure.Seeding;

/// <summary>
/// Creates one confirmed account per role so every screen can be opened from the first day of
/// development, without registering users by hand or editing the database directly.
/// </summary>
/// <remarks>
/// Idempotent: accounts are matched by e-mail and skipped if they already exist, so the seed
/// command can be re-run freely. The shared password is a development convenience and is why
/// this seeder refuses to run outside Development.
/// </remarks>
internal sealed class IdentitySeeder(
    IdentityDbContext dbContext,
    IPasswordHasher passwordHasher) : IDataSeeder
{
    /// <summary>Well known and intentionally weak. Never used outside a developer machine.</summary>
    public const string DevelopmentPassword = "Parola123!";

    /// <summary>Runs first: every other module's seed data references a user.</summary>
    public int Order => 10;

    public string Name => "identity";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var accounts = new (string Email, string First, string? Middle, string Last, UserRole Role)[]
        {
            ("admin@eduplatform.local", "Админ", null, "Администраторов", UserRole.Admin),
            ("teacher@eduplatform.local", "Мария", "Иванова", "Петрова", UserRole.Teacher),
            ("student@eduplatform.local", "Георги", "Петров", "Димитров", UserRole.Student),
            ("parent@eduplatform.local", "Петър", "Георгиев", "Димитров", UserRole.Parent),
        };

        var existing = await dbContext.Users
            .Select(user => user.EmailAddress)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingEmails = existing.ToHashSet(StringComparer.Ordinal);

        // One hash for all four: PBKDF2 is deliberately slow, and hashing the same password
        // four times would add seconds to every seed run for no benefit.
        var passwordHash = passwordHasher.Hash(DevelopmentPassword);

        foreach (var account in accounts)
        {
            var email = Email.Create(account.Email);
            if (existingEmails.Contains(email.Value))
            {
                continue;
            }

            var user = User.Register(
                email,
                PersonName.Create(account.First, account.Middle, account.Last),
                passwordHash,
                account.Role);

            // Skips the mail round-trip; these accounts must be usable immediately.
            user.ConfirmEmail();

            dbContext.Users.Add(user);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
