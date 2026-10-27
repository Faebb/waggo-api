using Waggo.Domain.Common;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Domain.Entities.Identity;

/// <summary>
/// An account (RF-001), shared by owners and walkers (ADR-016). Every confirmed user is an owner; the <c>walker</c>
/// role comes from an approved <see cref="Walkers.WalkerProfile"/>, and <see cref="IsAdmin"/> is seeded by hand.
/// </summary>
public sealed class User
{
    /// <summary>How long the 6-digit confirmation code works (a short code is only safe with a short life).</summary>
    public static readonly TimeSpan ConfirmationTokenLifetime = TimeSpan.FromMinutes(30);

    /// <summary>Wrong confirmation codes allowed before the code is burned and a new one must be requested.</summary>
    public const int MaxConfirmationAttempts = 5;

    /// <summary>Minimum time between two confirmation emails to the same account.</summary>
    public static readonly TimeSpan ConfirmationResendCooldown = TimeSpan.FromSeconds(60);

    /// <summary>How long the 6-digit code to recover a forgotten password works.</summary>
    public static readonly TimeSpan PasswordResetCodeLifetime = TimeSpan.FromMinutes(15);

    /// <summary>Wrong recovery codes allowed before the code is burned and a new one must be requested.</summary>
    public const int MaxPasswordResetAttempts = 5;

    /// <summary>Minimum time between two recovery emails to the same account.</summary>
    public static readonly TimeSpan PasswordResetResendCooldown = TimeSpan.FromSeconds(60);

    public const int MaxFailedLoginAttempts = 5;

    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public const int MaxFullNameLength = 100;

    public const int MaxPhoneLength = 20;

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

    /// <summary>Hash of the 6-digit confirmation code (RF-001); never the raw code. Cleared once confirmed.</summary>
    public string? ConfirmationTokenHash { get; private set; }

    public DateTimeOffset? ConfirmationTokenExpiresAt { get; private set; }

    /// <summary>Wrong codes typed for the current confirmation code; reset when a new one is issued.</summary>
    public int ConfirmationAttempts { get; private set; }

    /// <summary>Hash of the 6-digit password recovery code; never the raw code. Cleared once used.</summary>
    public string? PasswordResetCodeHash { get; private set; }

    public DateTimeOffset? PasswordResetCodeExpiresAt { get; private set; }

    /// <summary>Wrong codes typed for the current recovery code; reset when a new one is issued.</summary>
    public int PasswordResetAttempts { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }

    /// <summary>Optional, shown on the owner/walker's profile. Unset for a brand-new account.</summary>
    public string? FullName { get; private set; }

    /// <summary>Optional, shown on the owner/walker's profile. Unset for a brand-new account.</summary>
    public string? Phone { get; private set; }

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

    /// <summary>
    /// A provisioned account that skips the confirmation email: for seeding a known admin in Development/Testing,
    /// never for the RF-001 self-registration flow (<see cref="Register"/>).
    /// </summary>
    public static User Seed(string email, string passwordHash, bool isAdmin, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User
        {
            Id = Guid.CreateVersion7(now),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            EmailConfirmed = true,
            IsAdmin = isAdmin,
            RegisteredAt = now,
        };
    }

    /// <summary>
    /// Activates the account once the owner of the inbox presents the 6-digit code (its hash). A wrong code counts an
    /// attempt: after <see cref="MaxConfirmationAttempts"/> even the right one is refused until a new code is issued.
    /// </summary>
    public WaggoResponse<User> Confirm(string codeHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeHash);
        WaggoResponse<User> response = new();

        if (ConfirmationTokenHash is null)
        {
            response.AddError(IdentityErrors.ConfirmationTokenInvalid);
            return response;
        }

        if (now > ConfirmationTokenExpiresAt)
        {
            response.AddError(IdentityErrors.ConfirmationTokenExpired);
            return response;
        }

        if (ConfirmationAttempts >= MaxConfirmationAttempts)
        {
            response.AddError(IdentityErrors.ConfirmationAttemptsExceeded);
            return response;
        }

        if (ConfirmationTokenHash != codeHash)
        {
            ConfirmationAttempts++;
            response.AddError(IdentityErrors.ConfirmationTokenInvalid);
            return response;
        }

        EmailConfirmed = true;
        ConfirmationTokenHash = null;
        ConfirmationTokenExpiresAt = null;
        ConfirmationAttempts = 0;
        response.Data = this;
        return response;
    }

    /// <summary>
    /// Replaces the pending code with a new one (resend). Refused, changing nothing, when the account is already
    /// confirmed or the last code was sent less than <see cref="ConfirmationResendCooldown"/> ago.
    /// </summary>
    public bool TryIssueConfirmationCode(string codeHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeHash);

        if (EmailConfirmed)
        {
            return false;
        }

        if (ConfirmationTokenExpiresAt is DateTimeOffset expiresAt
            && now < expiresAt - ConfirmationTokenLifetime + ConfirmationResendCooldown)
        {
            return false;
        }

        ConfirmationTokenHash = codeHash;
        ConfirmationTokenExpiresAt = now.Add(ConfirmationTokenLifetime);
        ConfirmationAttempts = 0;
        return true;
    }

    /// <summary>
    /// Replaces the pending recovery code with a new one. Refused, changing nothing, when the account is not
    /// confirmed (the inbox is not proven yet) or the last code was sent less than
    /// <see cref="PasswordResetResendCooldown"/> ago.
    /// </summary>
    public bool TryIssuePasswordResetCode(string codeHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeHash);

        if (!EmailConfirmed)
        {
            return false;
        }

        if (PasswordResetCodeExpiresAt is DateTimeOffset expiresAt
            && now < expiresAt - PasswordResetCodeLifetime + PasswordResetResendCooldown)
        {
            return false;
        }

        PasswordResetCodeHash = codeHash;
        PasswordResetCodeExpiresAt = now.Add(PasswordResetCodeLifetime);
        PasswordResetAttempts = 0;
        return true;
    }

    /// <summary>
    /// Sets the new password once the owner of the inbox presents the recovery code (its hash). A wrong code counts
    /// an attempt: after <see cref="MaxPasswordResetAttempts"/> even the right one is refused until a new code is
    /// issued. Whoever controls the inbox is the owner again, so the lockout from failed logins is lifted too.
    /// </summary>
    public WaggoResponse<User> ResetPassword(string codeHash, string newPasswordHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPasswordHash);
        WaggoResponse<User> response = new();

        if (PasswordResetCodeHash is null)
        {
            response.AddError(IdentityErrors.ResetCodeInvalid);
            return response;
        }

        if (now > PasswordResetCodeExpiresAt)
        {
            response.AddError(IdentityErrors.ResetCodeExpired);
            return response;
        }

        if (PasswordResetAttempts >= MaxPasswordResetAttempts)
        {
            response.AddError(IdentityErrors.ResetAttemptsExceeded);
            return response;
        }

        if (PasswordResetCodeHash != codeHash)
        {
            PasswordResetAttempts++;
            response.AddError(IdentityErrors.ResetCodeInvalid);
            return response;
        }

        PasswordHash = newPasswordHash;
        PasswordResetCodeHash = null;
        PasswordResetCodeExpiresAt = null;
        PasswordResetAttempts = 0;
        FailedLoginAttempts = 0;
        LockedUntil = null;
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

    /// <summary>Replaces the editable part of the profile; a null clears that field.</summary>
    public void UpdateProfile(string? fullName, string? phone)
    {
        FullName = fullName;
        Phone = phone;
    }
}
