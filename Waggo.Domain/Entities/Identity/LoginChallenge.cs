using Waggo.Domain.Common;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Domain.Entities.Identity;

/// <summary>
/// Second factor of RF-005: once the password is right, a 6-digit code goes to the user's email and must be
/// verified before a session is issued.
/// </summary>
public sealed class LoginChallenge
{
    public const int MaxAttempts = 5;

    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    // Used by EF Core to materialize the entity.
    private LoginChallenge()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string CodeHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public int Attempts { get; private set; }

    public bool Used { get; private set; }

    public static LoginChallenge Create(Guid userId, string codeHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeHash);
        return new LoginChallenge
        {
            Id = Guid.CreateVersion7(now),
            UserId = userId,
            CodeHash = codeHash,
            ExpiresAt = now.Add(Lifetime),
            Attempts = 0,
            Used = false,
        };
    }

    /// <summary>Checks the code; a wrong one counts as an attempt, exhausting them locks the challenge for good.</summary>
    public WaggoResponse<LoginChallenge> Verify(string codeHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeHash);
        WaggoResponse<LoginChallenge> response = new();

        if (Used)
        {
            response.AddError(IdentityErrors.ChallengeNotFound);
            return response;
        }

        if (now > ExpiresAt)
        {
            response.AddError(IdentityErrors.CodeExpired);
            return response;
        }

        if (CodeHash != codeHash)
        {
            Attempts++;
            if (Attempts >= MaxAttempts)
            {
                Used = true;
                response.AddError(IdentityErrors.TooManyAttempts);
                return response;
            }

            response.AddError(IdentityErrors.InvalidCode);
            return response;
        }

        Used = true;
        response.Data = this;
        return response;
    }
}
