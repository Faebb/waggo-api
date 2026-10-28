using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;

namespace Waggo.Domain.UnitTests.Entities.Identity;

/// <summary>Accounts that come from Google or Microsoft: the provider already verified the email (ADR-017).</summary>
public class UserExternalTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RegisterExternal_ValidEmail_CreatesAConfirmedUserWithTheGivenHash()
    {
        WaggoResponse<User> result = User.RegisterExternal("  Ana@Mail.COM ", "unusable-hash", s_now);

        result.IsValid.ShouldBeTrue();
        result.Data.Email.ShouldBe("ana@mail.com");
        result.Data.EmailConfirmed.ShouldBeTrue();
        result.Data.PasswordHash.ShouldBe("unusable-hash");
        result.Data.IsAdmin.ShouldBeFalse();
        result.Data.ConfirmationTokenHash.ShouldBeNull();
        result.Data.RegisteredAt.ShouldBe(s_now);
    }

    [Fact]
    public void RegisterExternal_BlankEmail_FailsWithInvalidEmail()
    {
        WaggoResponse<User> result = User.RegisterExternal("   ", "unusable-hash", s_now);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.Code == "Identity.InvalidEmail");
    }

    [Fact]
    public void ConfirmByExternalProvider_UnconfirmedAccount_ConfirmsItAndReplacesThePassword()
    {
        // Someone registered the address with a password they know, but never proved they own the inbox.
        User user = User.Register("ana@mail.com", "attacker-password-hash", "code-hash", s_now).Data;

        user.ConfirmByExternalProvider("unusable-hash");

        user.EmailConfirmed.ShouldBeTrue();
        user.PasswordHash.ShouldBe("unusable-hash");
        user.ConfirmationTokenHash.ShouldBeNull();
        user.ConfirmationTokenExpiresAt.ShouldBeNull();
    }

    [Fact]
    public void ConfirmByExternalProvider_AlreadyConfirmedAccount_KeepsItsPassword()
    {
        User user = User.Register("ana@mail.com", "real-password-hash", "code-hash", s_now).Data;
        user.Confirm("code-hash", s_now);

        user.ConfirmByExternalProvider("unusable-hash");

        user.EmailConfirmed.ShouldBeTrue();
        user.PasswordHash.ShouldBe("real-password-hash");
    }
}
