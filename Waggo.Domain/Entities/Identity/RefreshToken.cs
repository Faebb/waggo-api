using Waggo.Domain.Common;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Domain.Entities.Identity;

/// <summary>
/// Opaque, rotating session token (RNF-002): every use replaces it with a new one and revokes this one. Presenting
/// an already-revoked token again is reuse of a stolen token, not a timing mistake — the caller must revoke the
/// whole family (<c>IRefreshTokenRepository.RevokeAllForUserAsync</c>).
/// </summary>
public sealed class RefreshToken
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    // Used by EF Core to materialize the entity.
    private RefreshToken()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now <= ExpiresAt;

    public static RefreshToken Create(Guid userId, string tokenHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        return new RefreshToken
        {
            Id = Guid.CreateVersion7(now),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = now.Add(Lifetime),
        };
    }

    /// <summary>Marks this token used and linked to the one that replaces it. Fails if it was already rotated or expired.</summary>
    public WaggoResponse<RefreshToken> Rotate(Guid replacedByTokenId, DateTimeOffset now)
    {
        WaggoResponse<RefreshToken> response = new();
        if (!IsActive(now))
        {
            response.AddError(IdentityErrors.RefreshTokenInvalid);
            return response;
        }

        RevokedAt = now;
        ReplacedByTokenId = replacedByTokenId;
        response.Data = this;
        return response;
    }

    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;
}
