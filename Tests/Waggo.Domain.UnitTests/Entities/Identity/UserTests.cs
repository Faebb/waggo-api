using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Enums.Common;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Domain.UnitTests.Entities.Identity;

public class UserTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Register_ValidEmail_CreatesAnUnconfirmedAccountWithTheTokenHash()
    {
        WaggoResponse<User> result = User.Register("Ana@Mail.com", "hashed-password", "token-hash", s_now);

        result.IsValid.ShouldBeTrue();
        result.Data.Email.ShouldBe("ana@mail.com");
        result.Data.PasswordHash.ShouldBe("hashed-password");
        result.Data.EmailConfirmed.ShouldBeFalse();
        result.Data.IsAdmin.ShouldBeFalse();
        result.Data.ConfirmationTokenHash.ShouldBe("token-hash");
        result.Data.ConfirmationTokenExpiresAt.ShouldBe(s_now.Add(User.ConfirmationTokenLifetime));
    }

    [Fact]
    public void Register_ValidEmail_StartsWithNoFullNameOrPhone()
    {
        WaggoResponse<User> result = User.Register("ana@mail.com", "hashed-password", "token-hash", s_now);

        result.Data.FullName.ShouldBeNull();
        result.Data.Phone.ShouldBeNull();
    }

    [Fact]
    public void UpdateProfile_FullNameAndPhone_SetsBoth()
    {
        User user = User.Register("ana@mail.com", "hashed-password", "token-hash", s_now).Data;

        user.UpdateProfile("Ana Pérez", "3001234567");

        user.FullName.ShouldBe("Ana Pérez");
        user.Phone.ShouldBe("3001234567");
    }

    [Fact]
    public void UpdateProfile_NullValues_ClearsBoth()
    {
        User user = User.Register("ana@mail.com", "hashed-password", "token-hash", s_now).Data;
        user.UpdateProfile("Ana Pérez", "3001234567");

        user.UpdateProfile(null, null);

        user.FullName.ShouldBeNull();
        user.Phone.ShouldBeNull();
    }

    [Fact]
    public void Register_EmptyEmail_FailsWithInvalidEmail()
    {
        WaggoResponse<User> result = User.Register("   ", "hashed-password", "token-hash", s_now);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.InvalidEmail.Code);
    }

    [Fact]
    public void Confirm_MatchingTokenBeforeExpiry_ActivatesTheAccountAndClearsTheToken()
    {
        User user = User.Register("ana@mail.com", "hashed-password", "token-hash", s_now).Data;

        WaggoResponse<User> result = user.Confirm("token-hash", s_now.Add(User.ConfirmationTokenLifetime));

        result.IsValid.ShouldBeTrue();
        user.EmailConfirmed.ShouldBeTrue();
        user.ConfirmationTokenHash.ShouldBeNull();
        user.ConfirmationTokenExpiresAt.ShouldBeNull();
    }

    [Fact]
    public void Confirm_WrongTokenHash_FailsWithoutActivating()
    {
        User user = User.Register("ana@mail.com", "hashed-password", "token-hash", s_now).Data;

        WaggoResponse<User> result = user.Confirm("another-hash", s_now);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.ConfirmationTokenInvalid.Code);
        user.EmailConfirmed.ShouldBeFalse();
    }

    [Fact]
    public void Confirm_AfterExpiry_FailsWithExpiredAndBusinessRuleType()
    {
        User user = User.Register("ana@mail.com", "hashed-password", "token-hash", s_now).Data;

        WaggoResponse<User> result =
            user.Confirm("token-hash", s_now.Add(User.ConfirmationTokenLifetime).AddSeconds(1));

        result.Errors.Single().Code.ShouldBe(IdentityErrors.ConfirmationTokenExpired.Code);
        result.ErrorType.ShouldBe(ErrorType.Conflict);
        user.EmailConfirmed.ShouldBeFalse();
    }

    [Fact]
    public void Seed_AnyEmail_CreatesAnAlreadyConfirmedAccountWithoutAConfirmationToken()
    {
        User user = User.Seed("Admin@Mail.com", "hashed-password", isAdmin: true, s_now);

        user.Email.ShouldBe("admin@mail.com");
        user.PasswordHash.ShouldBe("hashed-password");
        user.EmailConfirmed.ShouldBeTrue();
        user.ConfirmationTokenHash.ShouldBeNull();
        user.ConfirmationTokenExpiresAt.ShouldBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Seed_IsAdminFlag_IsStoredAsGiven(bool isAdmin)
    {
        User user = User.Seed("ana@mail.com", "hashed-password", isAdmin, s_now);

        user.IsAdmin.ShouldBe(isAdmin);
    }

    [Fact]
    public void RegisterFailedLoginAttempt_BelowTheMax_JustCounts()
    {
        User user = User.Register("ana@mail.com", "hash", "token-hash", s_now).Data;

        for (int i = 0; i < User.MaxFailedLoginAttempts - 1; i++)
        {
            user.RegisterFailedLoginAttempt(s_now);
        }

        user.FailedLoginAttempts.ShouldBe(User.MaxFailedLoginAttempts - 1);
        user.IsLocked(s_now).ShouldBeFalse();
    }

    [Fact]
    public void RegisterFailedLoginAttempt_ReachingTheMax_LocksTheAccountAndResetsTheCounter()
    {
        User user = User.Register("ana@mail.com", "hash", "token-hash", s_now).Data;

        for (int i = 0; i < User.MaxFailedLoginAttempts; i++)
        {
            user.RegisterFailedLoginAttempt(s_now);
        }

        user.FailedLoginAttempts.ShouldBe(0);
        user.IsLocked(s_now).ShouldBeTrue();
        user.IsLocked(s_now.Add(User.LockoutDuration).AddSeconds(1)).ShouldBeFalse();
    }

    [Fact]
    public void ResetFailedLoginAttempts_ClearsTheCounter()
    {
        User user = User.Register("ana@mail.com", "hash", "token-hash", s_now).Data;
        user.RegisterFailedLoginAttempt(s_now);

        user.ResetFailedLoginAttempts();

        user.FailedLoginAttempts.ShouldBe(0);
    }
}
