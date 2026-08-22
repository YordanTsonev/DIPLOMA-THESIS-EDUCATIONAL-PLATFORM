using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Identity.Domain;
using Shouldly;

namespace EduPlatform.Modules.Identity.UnitTests;

public sealed class UserTests
{
    [Fact]
    public void Registering_raises_the_event_other_modules_listen_for()
    {
        var user = CreateUser(UserRole.Student);

        user.DomainEvents.OfType<UserRegistered>().ShouldHaveSingleItem()
            .Role.ShouldBe(UserRole.Student);
        user.IsActive.ShouldBeTrue();
        user.EmailConfirmed.ShouldBeFalse();
    }

    [Fact]
    public void Changing_role_to_the_same_value_raises_nothing()
    {
        var user = CreateUser(UserRole.Teacher);
        user.ClearDomainEvents();

        user.ChangeRole(UserRole.Teacher);

        user.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Changing_role_records_where_it_came_from()
    {
        var user = CreateUser(UserRole.Student);
        user.ClearDomainEvents();

        user.ChangeRole(UserRole.Teacher);

        var changed = user.DomainEvents.OfType<UserRoleChanged>().ShouldHaveSingleItem();
        changed.PreviousRole.ShouldBe(UserRole.Student);
        changed.NewRole.ShouldBe(UserRole.Teacher);
    }

    [Fact]
    public void Changing_the_password_ends_every_other_session()
    {
        var user = CreateUser(UserRole.Student);
        user.IssueRefreshToken("hash-a", Later);
        user.IssueRefreshToken("hash-b", Later);

        user.ChangePassword("new-hash");

        user.RefreshTokens.ShouldAllBe(token => token.IsRevoked);
    }

    [Fact]
    public void Deactivating_revokes_sessions_and_blocks_new_ones()
    {
        var user = CreateUser(UserRole.Student);
        user.IssueRefreshToken("hash-a", Later);

        user.Deactivate();

        user.IsActive.ShouldBeFalse();
        user.RefreshTokens.ShouldAllBe(token => token.IsRevoked);
        Should.Throw<DomainException>(() => user.IssueRefreshToken("hash-b", Later));
    }

    [Fact]
    public void Rotating_a_token_revokes_the_old_one_and_links_the_two()
    {
        var user = CreateUser(UserRole.Student);
        user.IssueRefreshToken("old", Later);

        var replacement = user.RotateRefreshToken("old", "new", Later, Now);

        replacement.TokenHash.ShouldBe("new");
        var old = user.RefreshTokens.Single(token => token.TokenHash == "old");
        old.IsRevoked.ShouldBeTrue();
        old.ReplacedByTokenHash.ShouldBe("new");
        replacement.IsUsable(Now).ShouldBeTrue();
    }

    [Fact]
    public void Reusing_an_already_rotated_token_revokes_every_session()
    {
        // A token presented twice means a copy is in someone else's hands. The safe response is
        // to end all sessions rather than to guess which holder is the legitimate one.
        var user = CreateUser(UserRole.Student);
        user.IssueRefreshToken("old", Later);
        user.RotateRefreshToken("old", "new", Later, Now);

        Should.Throw<DomainException>(() => user.RotateRefreshToken("old", "another", Later, Now));

        user.RefreshTokens.ShouldAllBe(token => token.IsRevoked);
    }

    [Fact]
    public void An_expired_token_cannot_be_rotated()
    {
        var user = CreateUser(UserRole.Student);
        user.IssueRefreshToken("old", Now.AddMinutes(-1));

        Should.Throw<DomainException>(() => user.RotateRefreshToken("old", "new", Later, Now));
    }

    [Fact]
    public void An_unknown_token_is_rejected()
    {
        var user = CreateUser(UserRole.Student);

        Should.Throw<DomainException>(() => user.RotateRefreshToken("never-issued", "new", Later, Now));
    }

    [Fact]
    public void Confirming_the_email_twice_raises_one_event()
    {
        var user = CreateUser(UserRole.Student);
        user.ClearDomainEvents();

        user.ConfirmEmail();
        user.ConfirmEmail();

        user.DomainEvents.OfType<UserEmailConfirmed>().ShouldHaveSingleItem();
        user.EmailConfirmed.ShouldBeTrue();
    }

    private static DateTimeOffset Now => new(2026, 9, 15, 8, 0, 0, TimeSpan.Zero);

    private static DateTimeOffset Later => Now.AddDays(14);

    private static User CreateUser(UserRole role) => User.Register(
        Email.Create("ivan@example.com"),
        PersonName.Create("Иван", "Петров", "Георгиев"),
        "hashed-password",
        role);
}
