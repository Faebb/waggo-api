using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Domain.UnitTests.Entities.Identity;

/// <summary>A signed-in account changes its email, proving it owns the new inbox with a 6-digit code (RF-005).</summary>
public class UserEmailChangeTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    private static User ConfirmedUser() => User.Seed("ana@mail.com", "password-hash", isAdmin: false, s_now);

    private static User UserWithPendingChange()
    {
        User user = ConfirmedUser();
        user.RequestEmailChange("ana.nueva@mail.com", "code-hash", s_now);
        return user;
    }

    [Fact]
    public void RequestEmailChange_NewEmail_KeepsTheCurrentOneUntilConfirmed()
    {
        User user = ConfirmedUser();

        user.RequestEmailChange("ana.nueva@mail.com", "code-hash", s_now);

        user.Email.ShouldBe("ana@mail.com");
        user.PendingEmail.ShouldBe("ana.nueva@mail.com");
        user.EmailChangeCodeHash.ShouldBe("code-hash");
        user.EmailChangeCodeExpiresAt.ShouldBe(s_now.AddMinutes(15));
        user.EmailChangeAttempts.ShouldBe(0);
    }

    [Fact]
    public void RequestEmailChange_SecondRequest_ReplacesThePendingEmailAndCodeAndClearsTheAttempts()
    {
        User user = UserWithPendingChange();
        user.ConfirmEmailChange("wrong-hash", s_now.AddMinutes(1));

        user.RequestEmailChange("otra@mail.com", "second-hash", s_now.AddMinutes(2));

        user.PendingEmail.ShouldBe("otra@mail.com");
        user.EmailChangeCodeHash.ShouldBe("second-hash");
        user.EmailChangeAttempts.ShouldBe(0);
    }

    [Fact]
    public void ConfirmEmailChange_RightCode_AppliesTheNewEmailAndClearsThePendingChange()
    {
        User user = UserWithPendingChange();

        WaggoResponse<User> result = user.ConfirmEmailChange("code-hash", s_now.AddMinutes(5));

        result.IsValid.ShouldBeTrue();
        user.Email.ShouldBe("ana.nueva@mail.com");
        user.EmailConfirmed.ShouldBeTrue();
        user.PendingEmail.ShouldBeNull();
        user.EmailChangeCodeHash.ShouldBeNull();
        user.EmailChangeCodeExpiresAt.ShouldBeNull();
        user.EmailChangeAttempts.ShouldBe(0);
    }

    [Fact]
    public void ConfirmEmailChange_WrongCode_IsInvalidCountsAnAttemptAndKeepsTheEmail()
    {
        User user = UserWithPendingChange();

        WaggoResponse<User> result = user.ConfirmEmailChange("other-hash", s_now.AddMinutes(1));

        result.Errors.ShouldContain(error => error.Code == IdentityErrors.EmailChangeCodeInvalid.Code);
        user.EmailChangeAttempts.ShouldBe(1);
        user.Email.ShouldBe("ana@mail.com");
    }

    [Fact]
    public void ConfirmEmailChange_NothingPending_IsInvalid()
    {
        User user = ConfirmedUser();

        WaggoResponse<User> result = user.ConfirmEmailChange("code-hash", s_now);

        result.Errors.ShouldContain(error => error.Code == IdentityErrors.EmailChangeCodeInvalid.Code);
    }

    [Fact]
    public void ConfirmEmailChange_AfterFifteenMinutes_IsExpired()
    {
        User user = UserWithPendingChange();

        WaggoResponse<User> result = user.ConfirmEmailChange("code-hash", s_now.AddMinutes(16));

        result.Errors.ShouldContain(error => error.Code == IdentityErrors.EmailChangeCodeExpired.Code);
        user.Email.ShouldBe("ana@mail.com");
    }

    [Fact]
    public void ConfirmEmailChange_FiveWrongCodes_RefusesEvenTheRightOne()
    {
        User user = UserWithPendingChange();
        for (int attempt = 0; attempt < User.MaxEmailChangeAttempts; attempt++)
        {
            user.ConfirmEmailChange("other-hash", s_now.AddMinutes(1));
        }

        WaggoResponse<User> result = user.ConfirmEmailChange("code-hash", s_now.AddMinutes(2));

        result.Errors.ShouldContain(error => error.Code == IdentityErrors.EmailChangeAttemptsExceeded.Code);
        user.Email.ShouldBe("ana@mail.com");
    }
}
