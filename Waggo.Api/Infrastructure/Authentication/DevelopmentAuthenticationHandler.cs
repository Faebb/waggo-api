using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Waggo.Api.Infrastructure.Settings;

namespace Waggo.Api.Infrastructure.Authentication;

/// <summary>
/// Development-only authentication: every request is signed in as the configured development user, so no token
/// is needed. The roles can be overridden per request with the <c>X-Dev-Roles</c> header (comma separated) to try
/// the role-based rules, e.g. <c>X-Dev-Roles: walker</c>.
/// </summary>
internal sealed class DevelopmentAuthenticationHandler(
    IOptionsMonitor<DevelopmentAuthenticationOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder)
    : AuthenticationHandler<DevelopmentAuthenticationOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "Development";
    public const string RolesHeader = "X-Dev-Roles";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        DevelopmentUserSettings user = Options.User;
        string? rolesHeader = Request.Headers[RolesHeader];
        string[] roles = string.IsNullOrWhiteSpace(rolesHeader)
            ? user.Roles
            : rolesHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Name),
            .. roles.Select(role => new Claim(ClaimTypes.Role, role)),
        ];

        ClaimsPrincipal principal = new(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
