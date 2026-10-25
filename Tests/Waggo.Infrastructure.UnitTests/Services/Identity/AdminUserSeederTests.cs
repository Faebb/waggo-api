using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Entities.Identity;
using Waggo.Infrastructure.Options.Identity;
using Waggo.Infrastructure.Services.Identity;

namespace Waggo.Infrastructure.UnitTests.Services.Identity;

/// <summary>Development/Testing only: a real admin account to sign in with instead of the dev-user bypass.</summary>
public class AdminUserSeederTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly FakeTimeProvider _timeProvider = new(s_now);

    private AdminUserSeeder Sut(AdminSeedOptions options) =>
        new(_users, _passwordHasher, Microsoft.Extensions.Options.Options.Create(options), _timeProvider);

    [Fact]
    public async Task SeedAsync_NoEmailConfigured_DoesNothing()
    {
        await Sut(new AdminSeedOptions { Email = null, Password = "Admin1234" }).SeedAsync(CancellationToken.None);

        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SeedAsync_NoPasswordConfigured_DoesNothing()
    {
        await Sut(new AdminSeedOptions { Email = "admin@waggo.local", Password = null })
            .SeedAsync(CancellationToken.None);

        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SeedAsync_EmailAlreadyRegistered_DoesNothingSoItIsSafeOnEveryRestart()
    {
        _users.ExistsByEmailAsync("admin@waggo.local", Arg.Any<CancellationToken>()).Returns(true);

        await Sut(new AdminSeedOptions { Email = "admin@waggo.local", Password = "Admin1234" })
            .SeedAsync(CancellationToken.None);

        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SeedAsync_NewAdmin_SavesAConfirmedAccountWithTheAdminRoleAndHashedPassword()
    {
        _passwordHasher.Hash("Admin1234").Returns("hashed-password");

        await Sut(new AdminSeedOptions { Email = "Admin@Waggo.Local", Password = "Admin1234" })
            .SeedAsync(CancellationToken.None);

        await _users.Received(1).AddAsync(
            Arg.Is<User>(user =>
                user.Email == "admin@waggo.local"
                && user.PasswordHash == "hashed-password"
                && user.EmailConfirmed
                && user.IsAdmin),
            Arg.Any<CancellationToken>());
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
