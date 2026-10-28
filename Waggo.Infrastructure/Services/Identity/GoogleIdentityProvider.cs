using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Application.Identity;
using Waggo.Domain.Common;
using Waggo.Domain.Enums.Identity;
using Waggo.Domain.Errors.Identity;
using Waggo.Domain.Exceptions;
using Waggo.Infrastructure.Options.Identity;

namespace Waggo.Infrastructure.Services.Identity;

/// <summary>
/// ADR-017: validates the <c>idToken</c> the phone got from Google. Google signs it with rotating keys published at
/// its OpenID discovery document; <c>configuration</c> caches and refreshes them. Only RS256 tokens issued by Google
/// for one of our client ids and still alive are accepted.
/// </summary>
public sealed class GoogleIdentityProvider(
    IOptions<GoogleAuthOptions> options,
    IConfigurationManager<OpenIdConnectConfiguration> configuration,
    TimeProvider timeProvider,
    ILogger<GoogleIdentityProvider> logger) : IExternalIdentityProvider
{
    public const string DiscoveryUrl = "https://accounts.google.com/.well-known/openid-configuration";

    private static readonly string[] s_issuers = ["https://accounts.google.com", "accounts.google.com"];
    private static readonly JsonWebTokenHandler s_handler = new();

    public ExternalProvider Provider => ExternalProvider.Google;

    public async Task<WaggoResponse<ExternalIdentity>> ValidateAsync(
        string idToken,
        CancellationToken cancellationToken)
    {
        WaggoResponse<ExternalIdentity> response = new();
        string[] clientIds = options.Value.ClientIds;
        if (clientIds.Length == 0)
        {
            logger.LogWarning("Authentication:Google:ClientIds is not configured: every Google token is refused.");
            response.AddError(IdentityErrors.ExternalTokenInvalid);
            return response;
        }

        OpenIdConnectConfiguration google;
        try
        {
            google = await configuration.GetConfigurationAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new ExternalServiceException(
                IdentityErrors.ExternalProviderUnavailable, "Could not read the signing keys of Google.", exception);
        }

        TokenValidationResult result = await s_handler.ValidateTokenAsync(idToken, TokenParameters(google, clientIds));
        if (!result.IsValid)
        {
            logger.LogInformation(result.Exception, "A Google idToken was refused.");
            response.AddError(IdentityErrors.ExternalTokenInvalid);
            return response;
        }

        string? subject = IdTokenClaims.Text(result, JwtRegisteredClaimNames.Sub);
        string? email = IdTokenClaims.Text(result, JwtRegisteredClaimNames.Email);
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email))
        {
            response.AddError(IdentityErrors.ExternalTokenInvalid);
            return response;
        }

        bool emailVerified =
            string.Equals(IdTokenClaims.Text(result, "email_verified"), "true", StringComparison.OrdinalIgnoreCase);
        response.Data = new ExternalIdentity(subject, email, emailVerified);
        return response;
    }

    private TokenValidationParameters TokenParameters(OpenIdConnectConfiguration google, string[] clientIds) => new()
    {
        ValidIssuers = s_issuers,
        ValidAudiences = clientIds,
        IssuerSigningKeys = google.SigningKeys,
        ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
        RequireSignedTokens = true,
        RequireExpirationTime = true,
        ValidateLifetime = true,
        LifetimeValidator = IdTokenClaims.Lifetime(timeProvider),
    };
}
