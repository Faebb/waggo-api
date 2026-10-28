using System.Text.RegularExpressions;
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
/// ADR-017: validates the <c>idToken</c> the phone or browser got from Microsoft (personal, work and school accounts,
/// the "common" tenant). The signing keys are the same for every tenant, but the issuer carries the user's tenant, so
/// it is checked by shape and must match the token's own <c>tid</c>. Microsoft says the <c>email</c> claim cannot be
/// trusted on its own, so the email counts as verified only for personal accounts or when the tenant proves the domain
/// (<c>xms_edov</c>); the person is identified by tenant and object id (<c>tid</c> + <c>oid</c>), never by email.
/// </summary>
public sealed partial class MicrosoftIdentityProvider(
    IOptions<MicrosoftAuthOptions> options,
    IConfigurationManager<OpenIdConnectConfiguration> configuration,
    TimeProvider timeProvider,
    ILogger<MicrosoftIdentityProvider> logger) : IExternalIdentityProvider
{
    public const string DiscoveryUrl = "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration";

    /// <summary>The tenant every personal Microsoft account (Outlook, Xbox, ...) belongs to.</summary>
    public const string PersonalAccountsTenant = "9188040d-6c67-4c5b-b112-36a304b66dad";

    private static readonly JsonWebTokenHandler s_handler = new();

    public ExternalProvider Provider => ExternalProvider.Microsoft;

    public async Task<WaggoResponse<ExternalIdentity>> ValidateAsync(
        string idToken,
        CancellationToken cancellationToken)
    {
        WaggoResponse<ExternalIdentity> response = new();
        string[] clientIds = options.Value.ClientIds;
        if (clientIds.Length == 0)
        {
            logger.LogWarning("Authentication:Microsoft:ClientIds is not configured: every Microsoft token is refused.");
            response.AddError(IdentityErrors.ExternalTokenInvalid);
            return response;
        }

        OpenIdConnectConfiguration microsoft;
        try
        {
            microsoft = await configuration.GetConfigurationAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new ExternalServiceException(
                IdentityErrors.ExternalProviderUnavailable, "Could not read the signing keys of Microsoft.", exception);
        }

        TokenValidationResult result =
            await s_handler.ValidateTokenAsync(idToken, TokenParameters(microsoft, clientIds));
        if (!result.IsValid)
        {
            logger.LogInformation(result.Exception, "A Microsoft idToken was refused.");
            response.AddError(IdentityErrors.ExternalTokenInvalid);
            return response;
        }

        string? tenant = IdTokenClaims.Text(result, "tid");
        string? objectId = IdTokenClaims.Text(result, "oid");
        string? email = IdTokenClaims.Text(result, JwtRegisteredClaimNames.Email);
        if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(objectId) || string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning("A Microsoft idToken lacks tid, oid or email: check the optional claims of the app registration.");
            response.AddError(IdentityErrors.ExternalTokenInvalid);
            return response;
        }

        bool domainVerified =
            string.Equals(IdTokenClaims.Text(result, "xms_edov"), "true", StringComparison.OrdinalIgnoreCase);
        bool emailVerified = tenant == PersonalAccountsTenant || domainVerified;
        response.Data = new ExternalIdentity($"{tenant}:{objectId}", email, emailVerified);
        return response;
    }

    [GeneratedRegex("^https://login\\.microsoftonline\\.com/(?<tenant>[0-9a-fA-F-]{36})/v2\\.0$")]
    private static partial Regex IssuerShape();

    private TokenValidationParameters TokenParameters(OpenIdConnectConfiguration microsoft, string[] clientIds) => new()
    {
        IssuerValidator = ValidateIssuer,
        ValidAudiences = clientIds,
        IssuerSigningKeys = microsoft.SigningKeys,
        ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
        RequireSignedTokens = true,
        RequireExpirationTime = true,
        ValidateLifetime = true,
        LifetimeValidator = IdTokenClaims.Lifetime(timeProvider),
    };

    /// <summary>The issuer is <c>https://login.microsoftonline.com/{tenant}/v2.0</c> and that tenant is the token's <c>tid</c>.</summary>
    private static string ValidateIssuer(string issuer, SecurityToken token, TokenValidationParameters parameters)
    {
        Match match = IssuerShape().Match(issuer ?? string.Empty);
        if (!match.Success || token is not JsonWebToken jwt || !jwt.TryGetPayloadValue("tid", out string? tenant)
            || !string.Equals(tenant, match.Groups["tenant"].Value, StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityTokenInvalidIssuerException("The issuer is not a Microsoft tenant of this token.")
            {
                InvalidIssuer = issuer,
            };
        }

        return issuer!;
    }
}
