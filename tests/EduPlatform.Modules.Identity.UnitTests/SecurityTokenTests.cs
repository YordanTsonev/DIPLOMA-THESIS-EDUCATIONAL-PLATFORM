using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Identity.Domain;
using Shouldly;

namespace EduPlatform.Modules.Identity.UnitTests;

public sealed class SecurityTokenTests
{
    [Fact]
    public void Issuing_a_second_token_spends_the_first_one()
    {
        // Asking for another reset mail must invalidate the link in the earlier one, or two
        // live links exist and the older mailbox copy keeps working.
        var user = CreateUser();
        user.IssueSecurityToken(UserTokenPurpose.PasswordReset, "first", Later);

        user.IssueSecurityToken(UserTokenPurpose.PasswordReset, "second", Later);

        user.SecurityTokens.Single(token => token.TokenHash == "first").IsConsumed.ShouldBeTrue();
        user.SecurityTokens.Single(token => token.TokenHash == "second").IsUsable(Now).ShouldBeTrue();
    }

    [Fact]
    public void Tokens_for_different_purposes_do_not_cancel_each_other()
    {
        var user = CreateUser();
        user.IssueSecurityToken(UserTokenPurpose.EmailConfirmation, "confirm", Later);

        user.IssueSecurityToken(UserTokenPurpose.PasswordReset, "reset", Later);

        user.SecurityTokens.Single(token => token.TokenHash == "confirm").IsUsable(Now).ShouldBeTrue();
    }

    [Fact]
    public void A_token_cannot_be_spent_twice()
    {
        var user = CreateUser();
        user.IssueSecurityToken(UserTokenPurpose.PasswordReset, "hash", Later);
        user.ConsumeSecurityToken(UserTokenPurpose.PasswordReset, "hash", Now);

        Should.Throw<DomainException>(
            () => user.ConsumeSecurityToken(UserTokenPurpose.PasswordReset, "hash", Now));
    }

    [Fact]
    public void An_expired_token_is_refused()
    {
        var user = CreateUser();
        user.IssueSecurityToken(UserTokenPurpose.PasswordReset, "hash", Now.AddMinutes(-1));

        Should.Throw<DomainException>(
            () => user.ConsumeSecurityToken(UserTokenPurpose.PasswordReset, "hash", Now));
    }

    [Fact]
    public void A_token_cannot_be_used_for_a_purpose_it_was_not_issued_for()
    {
        // Otherwise a confirmation link, which is mailed on registration, would double as a
        // password reset for whoever holds it.
        var user = CreateUser();
        user.IssueSecurityToken(UserTokenPurpose.EmailConfirmation, "hash", Later);

        Should.Throw<DomainException>(
            () => user.ConsumeSecurityToken(UserTokenPurpose.PasswordReset, "hash", Now));
    }

    [Fact]
    public void Resetting_the_password_also_confirms_the_address_and_ends_all_sessions()
    {
        // Reading the link proves control of the mailbox, so a reset settles confirmation too.
        var user = CreateUser();
        user.IssueRefreshToken("session", Later);

        user.ResetPassword("new-hash");

        user.EmailConfirmed.ShouldBeTrue();
        user.PasswordHash.ShouldBe("new-hash");
        user.RefreshTokens.ShouldAllBe(token => token.IsRevoked);
    }

    private static DateTimeOffset Now => new(2026, 9, 26, 8, 0, 0, TimeSpan.Zero);

    private static DateTimeOffset Later => Now.AddHours(2);

    private static User CreateUser() => User.Register(
        Email.Create("ivan@example.com"),
        PersonName.Create("Иван", "Петров", "Георгиев"),
        "hashed-password",
        UserRole.Student);
}

public sealed class AuditEntryTests
{
    [Fact]
    public void An_entry_records_who_did_what_to_whom()
    {
        var actor = Guid.CreateVersion7();
        var subject = Guid.CreateVersion7();

        var entry = AuditEntry.Record(
            AuditActions.RoleChanged,
            actorUserId: actor,
            subjectUserId: subject,
            detail: "Student -> Teacher",
            ipAddress: "127.0.0.1");

        entry.Action.ShouldBe("user.role_changed");
        entry.ActorUserId.ShouldBe(actor);
        entry.SubjectUserId.ShouldBe(subject);
        entry.Detail.ShouldBe("Student -> Teacher");
    }

    [Fact]
    public void A_failed_sign_in_has_no_actor_but_is_still_recorded()
    {
        var entry = AuditEntry.Record(AuditActions.SignInFailed, detail: "bad credentials");

        entry.ActorUserId.ShouldBeNull();
        entry.Action.ShouldBe("user.sign_in_failed");
    }

    [Fact]
    public void An_entry_without_an_action_name_is_rejected()
    {
        Should.Throw<DomainException>(() => AuditEntry.Record("  "));
    }
}
