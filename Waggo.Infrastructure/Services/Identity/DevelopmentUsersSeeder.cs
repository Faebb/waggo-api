using Microsoft.Extensions.Options;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Entities.Walkers;
using Waggo.Domain.Enums.Walkers;
using Waggo.Infrastructure.Options.Identity;

namespace Waggo.Infrastructure.Services.Identity;

/// <summary>
/// DEV-001: creates the admin, owner and walker test accounts configured in <see cref="SeedUsersOptions"/>, already
/// confirmed, and gives the walker an approved profile, so Development and Testing have a real sign-in for every role
/// instead of the development-user bypass (ADR-011). Idempotent: safe to run on every restart.
/// </summary>
public sealed class DevelopmentUsersSeeder(
    IUserRepository users,
    IWalkerProfileRepository walkerProfiles,
    IPasswordHasher passwordHasher,
    IOptions<SeedUsersOptions> options,
    TimeProvider timeProvider)
{
    /// <summary>Recorded as the reviewer of the seeded walker's profile.</summary>
    public const string ReviewerId = "seed";

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        SeedUsersOptions settings = options.Value;
        await SeedUserAsync(settings.Admin, isAdmin: true, cancellationToken);
        await SeedUserAsync(settings.Owner, isAdmin: false, cancellationToken);

        User? walker = await SeedUserAsync(settings.Walker, isAdmin: false, cancellationToken);
        if (walker is not null)
        {
            await SeedApprovedWalkerProfileAsync(walker, settings.Walker!, cancellationToken);
        }
    }

    /// <summary>The account, new or already there; null when it is not configured.</summary>
    private async Task<User?> SeedUserAsync(SeedUserOptions? account, bool isAdmin, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(account?.Email) || string.IsNullOrWhiteSpace(account.Password))
        {
            return null;
        }

        string normalizedEmail = account.Email.Trim().ToLowerInvariant();
        User? existing = await users.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        string passwordHash = passwordHasher.Hash(account.Password);
        User user = User.Seed(normalizedEmail, passwordHash, isAdmin, timeProvider.GetUtcNow());
        user.UpdateProfile(account.FullName, account.Phone);
        await users.AddAsync(user, cancellationToken);
        return user;
    }

    private async Task SeedApprovedWalkerProfileAsync(
        User walker,
        SeedUserOptions account,
        CancellationToken cancellationToken)
    {
        string userId = walker.Id.ToString();
        if (await walkerProfiles.GetByUserAsync(userId, cancellationToken) is not null)
        {
            return;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        WaggoResponse<WalkerProfile> registered = WalkerProfile.Register(
            userId,
            account.FullName ?? string.Empty,
            DocumentType.CC,
            account.DocumentNumber ?? string.Empty,
            account.Phone ?? string.Empty,
            experience: null,
            now);
        if (!registered.IsValid)
        {
            throw new InvalidOperationException(
                $"Seed:Users:Walker needs a valid FullName, Phone and DocumentNumber: "
                + string.Join(", ", registered.Errors.Select(error => error.Code)));
        }

        WalkerProfile profile = registered.Data!;
        profile.Approve(ReviewerId, now);
        await walkerProfiles.AddAsync(profile, cancellationToken);
    }
}
