using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Identity;
using Waggo.Domain.Common;
using Waggo.Domain.Enums.Identity;
using Waggo.Domain.Errors.Identity;

namespace Waggo.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Stands in for Google or Microsoft in the integration tests (no real provider is reachable from a test run). The
/// "token" is <c>ok|id|email|verified</c>; anything else is rejected, like a forged or expired <c>idToken</c>.
/// </summary>
public sealed class FakeExternalIdentityProvider(ExternalProvider provider) : IExternalIdentityProvider
{
    public ExternalProvider Provider => provider;

    public static string TokenFor(string providerUserId, string email, bool emailVerified = true) =>
        $"ok|{providerUserId}|{email}|{emailVerified}";

    public Task<WaggoResponse<ExternalIdentity>> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        WaggoResponse<ExternalIdentity> response = new();
        string[] parts = idToken.Split('|');
        if (parts.Length != 4 || parts[0] != "ok")
        {
            response.AddError(IdentityErrors.ExternalTokenInvalid);
            return Task.FromResult(response);
        }

        response.Data = new ExternalIdentity(parts[1], parts[2], bool.Parse(parts[3]));
        return Task.FromResult(response);
    }
}
