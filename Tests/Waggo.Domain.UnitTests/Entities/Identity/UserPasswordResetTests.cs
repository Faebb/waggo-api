using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Domain.UnitTests.Entities.Identity;

/// <summary>Recovery of a forgotten password with a 6-digit code (RF-001, RF-005).</summary>
public class UserPasswordResetTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    private static User ConfirmedUser() => User.Seed("ana@mail.com", "old-password-hash", isAdmin: false, s_now);

    private static User UserWithCode(string codeHash = "code-hash")
    {
        User user = ConfirmedUser();
        user.TryIssuePasswordResetCode(codeHash, s_now).ShouldBeTrue();
        return user;
    }

    [Fact]
    public void TryIssuePasswordResetCode_ConfirmedAccount_StoresTheHashAndAFifteenMinuteLife()
    {
        User user = ConfirmedUser();

        bool issued = user.TryIssuePasswordResetCode("code-hash", s_now);

        issued.ShouldBeTrue();
        user.PasswordResetCodeHash.ShouldBe("code-hash");
        user.PasswordResetCodeExpiresAt.ShouldBe(s_now.AddMinutes(15));
        user.PasswordResetAttempts.ShouldBe(0);
    }

    [Fact]
    public void TryIssuePasswordResetCode_UnconfirmedAccount_IssuesNothing()
    {
        User user = User.Register("ana@mail.com", "hash", "confirmation-hash", s_now).Data;

        bool issued = user.TryIssuePasswordResetCode("code-hash", s_now);

        issued.ShouldBeFalse();
        user.PasswordResetCodeHash.ShouldBeNull();
    }

    [Fact]
    public void TryIssuePasswordResetCode_BeforeTheWaitTime_KeepsTheCurrentCode()
    {
        User user = UserWithCode("first-hash");

        bool issued = user.TryIssuePasswordResetCode("second-hash", s_now.AddSeconds(30));

        issued.ShouldBeFalse();
        user.PasswordResetCodeHash.ShouldBe("first-hash");
    }

    [Fact]
    public void TryIssuePasswordResetCode_AfterTheWaitTime_ReplacesTheCodeAndClearsTheAttempts()
    {
        User user = UserWithCode("first-hash");
        user.ResetPassword("wrong-hash", "new-hash", s_now.AddSeconds(5));

        bool issued = user.TryIssuePasswordResetCode("second-hash", s_now.AddSeconds(61));

        issued.ShouldBeTrue();
        user.PasswordResetCodeHash.ShouldBe("second-hash");
        user.PasswordResetCodeExpiresAt.ShouldBe(s_now.AddSeconds(61).AddMinutes(15));
        user.PasswordResetAttempts.ShouldBe(0);
    }

    [Fact]
    public void ResetPassword_RightCode_ChangesThePasswordAndBurnsTheCode()
    {
        User user = UserWithCode();

        WaggoResponse<User> result = user.ResetPassword("code-hash", "new-password-hash", s_now.AddMinutes(5));

        result.IsValid.ShouldBeTrue();
        user.PasswordHash.ShouldBe("new-password-hash");
        user.PasswordResetCodeHash.ShouldBeNull();
        user.PasswordResetCodeExpiresAt.ShouldBeNull();
        user.PasswordResetAttempts.ShouldBe(0);
    }

    [Fact]
    public void ResetPassword_RightCode_LiftsTheLockoutFromFailedLogins()
    {
        User user = UserWithCode();
        for (int attempt = 0; attempt < User.MaxFailedLoginAttempts; attempt++)
        {
            user.RegisterFailedLoginAttempt(s_now);
        }

        user.IsLocked(s_now).ShouldBeTrue();

        user.ResetPassword("code-hash", "new-password-hash", s_now.AddMinutes(1));

        user.IsLocked(s_now.AddMinutes(1)).ShouldBeFalse();
    }

    [Fact]
    public void ResetPassword_RightCode_KeepsTheEmailConfirmed()
    {
        User user = UserWithCode();

        user.ResetPassword("code-hash", "new-password-hash", s_now.AddMinutes(1));

        user.EmailConfirmed.ShouldBeTrue();
    }

    [Fact]
    public void ResetPassword_WrongCode_IsInvalidCountsAnAttemptAndKeepsThePassword()
    {
        User user = UserWithCode();

        WaggoResponse<User> result = user.ResetPassword("other-hash", "new-password-hash", s_now.AddMinutes(1));

        result.Errors.ShouldContain(error => error.Code == IdentityErrors.ResetCodeInvalid.Code);
        user.PasswordResetAttempts.ShouldBe(1);
        user.PasswordHash.ShouldBe("old-password-hash");
        user.PasswordResetCodeHash.ShouldBe("code-hash");
    }

    [Fact]
    public void ResetPassword_NoCodeWasIssued_IsInvalid()
    {
        User user = ConfirmedUser();

        WaggoResponse<User> result = user.ResetPassword("code-hash", "new-password-hash", s_now);

        result.Errors.ShouldContain(error => error.Code == IdentityErrors.ResetCodeInvalid.Code);
    }

    [Fact]
    public void ResetPassword_AfterFifteenMinutes_IsExpired()
    {
        User user = UserWithCode();

        WaggoResponse<User> result = user.ResetPassword("code-hash", "new-password-hash", s_now.AddMinutes(16));

        result.Errors.ShouldContain(error => error.Code == IdentityErrors.ResetCodeExpired.Code);
        user.PasswordHash.ShouldBe("old-password-hash");
    }

    [Fact]
    public void ResetPassword_FiveWrongCodes_RefusesEvenTheRightOneUntilANewCodeIsIssued()
    {
        User user = UserWithCode();
        for (int attempt = 0; attempt < User.MaxPasswordResetAttempts; attempt++)
        {
            user.ResetPassword("other-hash", "new-password-hash", s_now.AddMinutes(1));
        }

        WaggoResponse<User> result = user.ResetPassword("code-hash", "new-password-hash", s_now.AddMinutes(2));

        result.Errors.ShouldContain(error => error.Code == IdentityErrors.ResetAttemptsExceeded.Code);
        user.PasswordHash.ShouldBe("old-password-hash");
    }
}
