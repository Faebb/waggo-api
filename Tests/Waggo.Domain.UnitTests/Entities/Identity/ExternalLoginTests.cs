using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Enums.Identity;

namespace Waggo.Domain.UnitTests.Entities.Identity;

public class ExternalLoginTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid s_userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [Fact]
    public void Create_KeepsTheProviderAndItsUserId()
    {
        ExternalLogin login = ExternalLogin.Create(s_userId, ExternalProvider.Google, "google-sub-123", s_now);

        login.UserId.ShouldBe(s_userId);
        login.Provider.ShouldBe(ExternalProvider.Google);
        login.ProviderUserId.ShouldBe("google-sub-123");
        login.CreatedAt.ShouldBe(s_now);
        login.Id.ShouldNotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankProviderUserId_Throws(string providerUserId)
    {
        Should.Throw<ArgumentException>(() =>
            ExternalLogin.Create(s_userId, ExternalProvider.Microsoft, providerUserId, s_now));
    }
}
