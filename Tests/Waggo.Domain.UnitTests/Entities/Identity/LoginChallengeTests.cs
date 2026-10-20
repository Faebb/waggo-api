using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Domain.UnitTests.Entities.Identity;

public class LoginChallengeTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid s_userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [Fact]
    public void Verify_MatchingCodeBeforeExpiry_SucceedsAndMarksUsed()
    {
        LoginChallenge challenge = LoginChallenge.Create(s_userId, "code-hash", s_now);

        WaggoResponse<LoginChallenge> result = challenge.Verify("code-hash", s_now);

        result.IsValid.ShouldBeTrue();
        challenge.Used.ShouldBeTrue();
    }

    [Fact]
    public void Verify_WrongCode_CountsAnAttemptAndFailsWithInvalidCode()
    {
        LoginChallenge challenge = LoginChallenge.Create(s_userId, "code-hash", s_now);

        WaggoResponse<LoginChallenge> result = challenge.Verify("wrong-hash", s_now);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.InvalidCode.Code);
        challenge.Attempts.ShouldBe(1);
        challenge.Used.ShouldBeFalse();
    }

    [Fact]
    public void Verify_ExhaustingAttempts_LocksTheChallengeWithTooManyAttempts()
    {
        LoginChallenge challenge = LoginChallenge.Create(s_userId, "code-hash", s_now);

        WaggoResponse<LoginChallenge> result = new();
        for (int i = 0; i < LoginChallenge.MaxAttempts; i++)
        {
            result = challenge.Verify("wrong-hash", s_now);
        }

        result.Errors.Single().Code.ShouldBe(IdentityErrors.TooManyAttempts.Code);
        challenge.Used.ShouldBeTrue();

        WaggoResponse<LoginChallenge> afterLockout = challenge.Verify("code-hash", s_now);
        afterLockout.Errors.Single().Code.ShouldBe(IdentityErrors.ChallengeNotFound.Code);
    }

    [Fact]
    public void Verify_AfterExpiry_FailsWithCodeExpired()
    {
        LoginChallenge challenge = LoginChallenge.Create(s_userId, "code-hash", s_now);

        WaggoResponse<LoginChallenge> result = challenge.Verify("code-hash", s_now.Add(LoginChallenge.Lifetime).AddSeconds(1));

        result.Errors.Single().Code.ShouldBe(IdentityErrors.CodeExpired.Code);
        challenge.Used.ShouldBeFalse();
    }

    [Fact]
    public void Verify_AlreadyUsed_FailsWithChallengeNotFound()
    {
        LoginChallenge challenge = LoginChallenge.Create(s_userId, "code-hash", s_now);
        challenge.Verify("code-hash", s_now);

        WaggoResponse<LoginChallenge> result = challenge.Verify("code-hash", s_now);

        result.Errors.Single().Code.ShouldBe(IdentityErrors.ChallengeNotFound.Code);
    }
}
