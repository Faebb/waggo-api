using Waggo.Application.Identity;
using Waggo.Domain.Common;
using Waggo.Domain.Enums.Identity;

namespace Waggo.Application.Common.Interfaces.Identity;

/// <summary>
/// Port for one external identity provider (Google, Microsoft; ADR-017). It checks the <c>idToken</c> the phone got
/// from the provider: signature, issuer, audience and lifetime. The API never sees the person's password there.
/// </summary>
public interface IExternalIdentityProvider
{
    ExternalProvider Provider { get; }

    /// <summary>A bad token (forged, expired, for another app) answers <c>Identity.ExternalTokenInvalid</c>.</summary>
    Task<WaggoResponse<ExternalIdentity>> ValidateAsync(string idToken, CancellationToken cancellationToken);
}
