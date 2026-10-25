namespace Waggo.Infrastructure.Options.Identity;

/// <summary>
/// Bound from "Seed:Admin". Both fields are optional: unset means no admin is seeded. Only read in Development and
/// Testing (<see cref="Services.Identity.AdminUserSeeder"/>), so a real account can sign in with every role instead
/// of the development-user bypass (ADR-011).
/// </summary>
public sealed class AdminSeedOptions
{
    public const string SectionName = "Seed:Admin";

    public string? Email { get; set; }

    public string? Password { get; set; }
}
