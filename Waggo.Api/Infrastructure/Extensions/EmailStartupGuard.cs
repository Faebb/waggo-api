namespace Waggo.Api.Infrastructure.Extensions;

/// <summary>
/// RF-001: the log-only email sender is for Development and Testing. Production must send real email, so the API
/// refuses to start otherwise (same idea as the development user guard in <c>AddWaggoAuthentication</c>).
/// </summary>
internal static class EmailStartupGuard
{
    private const string ProviderKey = "Email:Provider";

    public static void EnsureRealProviderInProduction(IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        if (!environment.IsProduction())
        {
            return;
        }

        string? provider = configuration[ProviderKey];
        if (!string.Equals(provider, "Ses", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Email:Provider must be 'Ses' in Production (with Email:FromAddress and Email:Ses:Region).");
        }
    }
}
