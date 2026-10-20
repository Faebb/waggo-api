using Waggo.Domain.Common;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Domain.Entities.Identity;

/// <summary>
/// An account (RF-001), shared by owners and walkers (ADR-016). Every confirmed user is an owner; the <c>walker</c>
/// role comes from an approved <see cref="Walkers.WalkerProfile"/>, and <see cref="IsAdmin"/> is seeded by hand.
/// </summary>
public sealed class User
{
    public static readonly TimeSpan ConfirmationTokenLifetime = TimeSpan.FromHours(24);

    public const int MaxFailedLoginAttempts = 5;

    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    // Used by EF Core to materialize the entity.
    private User()
    {
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    /// <summary>Produced by <c>IPasswordHasher</c> (RNF-001); never the plain text password.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    public bool EmailConfirmed { get; private set; }

    public bool IsAdmin { get; private set; }

    /// <summary>Hash of the confirmation token (RF-001); never the raw token. Cleared once confirmed.</summary>
    public string? ConfirmationTokenHash { get; private set; }

    public DateTimeOffset? ConfirmationTokenExpiresAt { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }

    public int FailedLoginAttempts { get; private set; }

    public DateTimeOffset? LockedUntil { get; private set; }

    public bool IsLocked(DateTimeOffset now) => LockedUntil > now;

    public static WaggoResponse<User> Register(
        string email,
        string passwordHash,
        string confirmationTokenHash,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(confirmationTokenHash);
        WaggoResponse<User> response = new();

        string normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalizedEmail.Length == 0)
        {
            response.AddError(IdentityErrors.InvalidEmail);
            return response;
        }

        response.Data = new User
        {
            Id = Guid.CreateVersion7(now),
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            EmailConfirmed = false,
            IsAdmin = false,
            ConfirmationTokenHash = confirmationTokenHash,
            ConfirmationTokenExpiresAt = now.Add(ConfirmationTokenLifetime),
            RegisteredAt = now,
        };
        return response;
    }

    /// <summary>Activates the account once the owner of the inbox proves it by presenting the token.</summary>
    public WaggoResponse<User> Confirm(string tokenHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        WaggoResponse<User> response = new();

        if (ConfirmationTokenHash is null || ConfirmationTokenHash != tokenHash)
        {
            response.AddError(IdentityErrors.ConfirmationTokenInvalid);
            return response;
        }

        if (now > ConfirmationTokenExpiresAt)
        {
            response.AddError(IdentityErrors.ConfirmationTokenExpired);
            return response;
        }

        EmailConfirmed = true;
        ConfirmationTokenHash = null;
        ConfirmationTokenExpiresAt = null;
        response.Data = this;
        return response;
    }

    /// <summary>A wrong password; locks the account for <see cref="LockoutDuration"/> after too many in a row.</summary>
    public void RegisterFailedLoginAttempt(DateTimeOffset now)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= MaxFailedLoginAttempts)
        {
            LockedUntil = now.Add(LockoutDuration);
            FailedLoginAttempts = 0;
        }
    }

    /// <summary>The password was right: the slate is clean for the next time it is wrong.</summary>
    public void ResetFailedLoginAttempts() => FailedLoginAttempts = 0;
}
