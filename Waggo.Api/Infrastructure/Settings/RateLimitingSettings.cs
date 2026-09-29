namespace Waggo.Api.Infrastructure.Settings;

/// <summary>"RateLimiting" section: fixed window per user (or per IP when anonymous).</summary>
public sealed class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    public int PermitLimit { get; set; } = 100;

    public int WindowSeconds { get; set; } = 60;
}
