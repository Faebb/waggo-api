using Microsoft.Extensions.Options;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Entities.Identity;
using Waggo.Infrastructure.Options.Identity;

namespace Waggo.Infrastructure.Services.Identity;

/// <summary>
/// Creates the one admin account configured in <see cref="AdminSeedOptions"/>, already confirmed, so Development
/// and Testing have a real sign-in to exercise instead of the development-user bypass (ADR-011). Idempotent: safe
/// to run on every restart.
/// </summary>
public sealed class AdminUserSeeder(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IOptions<AdminSeedOptions> options,
    TimeProvider timeProvider)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        AdminSeedOptions settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Email) || string.IsNullOrWhiteSpace(settings.Password))
        {
            return;
        }

        string normalizedEmail = settings.Email.Trim().ToLowerInvariant();
        if (await users.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            return;
        }

        string passwordHash = passwordHasher.Hash(settings.Password);
        User admin = User.Seed(normalizedEmail, passwordHash, isAdmin: true, timeProvider.GetUtcNow());
        await users.AddAsync(admin, cancellationToken);
    }
}
