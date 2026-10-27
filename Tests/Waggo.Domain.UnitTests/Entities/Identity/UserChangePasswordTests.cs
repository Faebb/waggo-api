using Waggo.Domain.Entities.Identity;

namespace Waggo.Domain.UnitTests.Entities.Identity;

/// <summary>A signed-in account changes its own password (RF-005).</summary>
public class UserChangePasswordTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    private static User ConfirmedUser() => User.Seed("ana@mail.com", "old-password-hash", isAdmin: false, s_now);

    [Fact]
    public void ChangePassword_NewHash_ReplacesThePasswordHash()
    {
        User user = ConfirmedUser();

        user.ChangePassword("new-password-hash");

        user.PasswordHash.ShouldBe("new-password-hash");
    }

    [Fact]
    public void ChangePassword_NewHash_ClearsTheFailedAttempts()
    {
        User user = ConfirmedUser();
        user.RegisterFailedLoginAttempt(s_now);
        user.RegisterFailedLoginAttempt(s_now);

        user.ChangePassword("new-password-hash");
        for (int attempt = 0; attempt < User.MaxFailedLoginAttempts - 1; attempt++)
        {
            user.RegisterFailedLoginAttempt(s_now);
        }

        user.IsLocked(s_now).ShouldBeFalse();
    }

    [Fact]
    public void ChangePassword_BlankHash_Throws()
    {
        User user = ConfirmedUser();

        Should.Throw<ArgumentException>(() => user.ChangePassword(" "));
    }
}
