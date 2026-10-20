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
}
