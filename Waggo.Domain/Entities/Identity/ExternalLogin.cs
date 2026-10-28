using Waggo.Domain.Enums.Identity;

namespace Waggo.Domain.Entities.Identity;

/// <summary>
/// A Google or Microsoft account linked to a <see cref="User"/> (ADR-017). It is found by the provider's own user id,
/// never by email: the email can change or be reused at the provider.
/// </summary>
public sealed class ExternalLogin
{
    // Used by EF Core to materialize the entity.
    private ExternalLogin()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public ExternalProvider Provider { get; private set; }

    /// <summary>The <c>sub</c> (Google) or <c>oid</c> (Microsoft) of the account at the provider.</summary>
    public string ProviderUserId { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public static ExternalLogin Create(
        Guid userId,
        ExternalProvider provider,
        string providerUserId,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerUserId);

        return new ExternalLogin
        {
            Id = Guid.CreateVersion7(now),
            UserId = userId,
            Provider = provider,
            ProviderUserId = providerUserId,
            CreatedAt = now,
        };
    }
}
