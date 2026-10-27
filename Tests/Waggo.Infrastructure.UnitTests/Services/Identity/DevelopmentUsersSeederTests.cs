using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Entities.Walkers;
using Waggo.Domain.Enums.Walkers;
using Waggo.Infrastructure.Options.Identity;
using Waggo.Infrastructure.Services.Identity;

namespace Waggo.Infrastructure.UnitTests.Services.Identity;

/// <summary>DEV-001: Development/Testing only, an admin, an owner and a verified walker to sign in with.</summary>
public class DevelopmentUsersSeederTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IWalkerProfileRepository _walkerProfiles = Substitute.For<IWalkerProfileRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly FakeTimeProvider _timeProvider = new(s_now);

    public DevelopmentUsersSeederTests()
    {
        _passwordHasher.Hash(Arg.Any<string>()).Returns(call => $"hashed-{call.Arg<string>()}");
    }

    private static SeedUsersOptions AllThree() => new()
    {
        Admin = new SeedUserOptions
        {
            Email = "Admin@Waggo.Local",
            Password = "Admin1234",
            FullName = "Administrador Waggo",
            Phone = "3000000001",
        },
        Owner = new SeedUserOptions
        {
            Email = "dueno@waggo.local",
            Password = "Dueno1234",
            FullName = "Laura Gómez",
            Phone = "3000000002",
        },
        Walker = new SeedUserOptions
        {
            Email = "paseador@waggo.local",
            Password = "Paseador1234",
            FullName = "Carlos Ramírez",
            Phone = "3000000003",
            DocumentNumber = "1000000003",
        },
    };

    private DevelopmentUsersSeeder Sut(SeedUsersOptions options) =>
        new(_users, _walkerProfiles, _passwordHasher, Microsoft.Extensions.Options.Options.Create(options), _timeProvider);

    [Fact]
    public async Task SeedAsync_EmptyDatabase_SavesTheThreeConfirmedAccountsWithNameAndPhone()
    {
        await Sut(AllThree()).SeedAsync(CancellationToken.None);

        await _users.Received(1).AddAsync(
            Arg.Is<User>(user =>
                user.Email == "admin@waggo.local"
                && user.PasswordHash == "hashed-Admin1234"
                && user.EmailConfirmed
                && user.IsAdmin
                && user.FullName == "Administrador Waggo"
                && user.Phone == "3000000001"),
            Arg.Any<CancellationToken>());
        await _users.Received(1).AddAsync(
            Arg.Is<User>(user =>
                user.Email == "dueno@waggo.local"
                && user.PasswordHash == "hashed-Dueno1234"
                && user.EmailConfirmed
                && !user.IsAdmin
                && user.FullName == "Laura Gómez"
                && user.Phone == "3000000002"),
            Arg.Any<CancellationToken>());
        await _users.Received(1).AddAsync(
            Arg.Is<User>(user =>
                user.Email == "paseador@waggo.local"
                && user.PasswordHash == "hashed-Paseador1234"
                && user.EmailConfirmed
                && !user.IsAdmin
                && user.FullName == "Carlos Ramírez"
                && user.Phone == "3000000003"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SeedAsync_EmptyDatabase_GivesTheWalkerAnApprovedProfileSoTheSessionHasTheWalkerRole()
    {
        User? savedWalker = null;
        await _users.AddAsync(
            Arg.Do<User>(user => savedWalker = user.Email == "paseador@waggo.local" ? user : savedWalker),
            Arg.Any<CancellationToken>());

        await Sut(AllThree()).SeedAsync(CancellationToken.None);

        savedWalker.ShouldNotBeNull();
        await _walkerProfiles.Received(1).AddAsync(
            Arg.Is<WalkerProfile>(profile =>
                profile.UserId == savedWalker.Id.ToString()
                && profile.IsVerified
                && profile.FullName == "Carlos Ramírez"
                && profile.DocumentType == DocumentType.CC
                && profile.DocumentNumber == "1000000003"
                && profile.Phone == "3000000003"
                && profile.ReviewedBy == DevelopmentUsersSeeder.ReviewerId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SeedAsync_AccountsAndProfileAlreadyExist_AddsNothingSoItIsSafeOnEveryRestart()
    {
        SeedUsersOptions options = AllThree();
        User existingWalker = User.Seed("paseador@waggo.local", "old-hash", isAdmin: false, s_now);
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(User.Seed("someone@waggo.local", "old-hash", isAdmin: false, s_now));
        _users.GetByEmailAsync("paseador@waggo.local", Arg.Any<CancellationToken>()).Returns(existingWalker);
        _walkerProfiles.GetByUserAsync(existingWalker.Id.ToString(), Arg.Any<CancellationToken>())
            .Returns(WalkerProfile.Register(
                existingWalker.Id.ToString(), "Carlos Ramírez", DocumentType.CC, "1000000003", "3000000003", null, s_now)
                .Data);

        await Sut(options).SeedAsync(CancellationToken.None);

        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _walkerProfiles.DidNotReceive().AddAsync(Arg.Any<WalkerProfile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SeedAsync_WalkerExistsWithoutProfile_AddsOnlyTheApprovedProfile()
    {
        User existingWalker = User.Seed("paseador@waggo.local", "old-hash", isAdmin: false, s_now);
        _users.GetByEmailAsync("paseador@waggo.local", Arg.Any<CancellationToken>()).Returns(existingWalker);

        await Sut(new SeedUsersOptions { Walker = AllThree().Walker }).SeedAsync(CancellationToken.None);

        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _walkerProfiles.Received(1).AddAsync(
            Arg.Is<WalkerProfile>(profile => profile.UserId == existingWalker.Id.ToString() && profile.IsVerified),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SeedAsync_AccountWithoutPassword_IsSkippedAndTheOthersAreStillSaved()
    {
        SeedUsersOptions options = AllThree();
        options.Owner!.Password = null;

        await Sut(options).SeedAsync(CancellationToken.None);

        await _users.DidNotReceive().AddAsync(
            Arg.Is<User>(user => user.Email == "dueno@waggo.local"), Arg.Any<CancellationToken>());
        await _users.Received(2).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SeedAsync_NothingConfigured_DoesNothing()
    {
        await Sut(new SeedUsersOptions()).SeedAsync(CancellationToken.None);

        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _walkerProfiles.DidNotReceive().AddAsync(Arg.Any<WalkerProfile>(), Arg.Any<CancellationToken>());
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
