using System.Globalization;
using Microsoft.IdentityModel.Tokens;

namespace Waggo.Infrastructure.Services.Identity;

/// <summary>What the Google and Microsoft <c>idToken</c> validators share: reading claims and judging the lifetime.</summary>
internal static class IdTokenClaims
{
    private static readonly TimeSpan s_clockSkew = TimeSpan.FromMinutes(2);

    /// <summary>A claim as text, or null. Providers send booleans as JSON booleans or as strings: read both.</summary>
    public static string? Text(TokenValidationResult result, string name) =>
        result.Claims.TryGetValue(name, out object? value)
            ? Convert.ToString(value, CultureInfo.InvariantCulture)
            : null;

    /// <summary>The token must be alive now (a couple of minutes of tolerance for clocks that differ).</summary>
    public static LifetimeValidator Lifetime(TimeProvider timeProvider) => (notBefore, expires, _, _) =>
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        return expires is not null && expires > now - s_clockSkew && (notBefore is null || notBefore <= now + s_clockSkew);
    };
}
