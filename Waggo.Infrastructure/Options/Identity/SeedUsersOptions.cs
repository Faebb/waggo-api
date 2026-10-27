namespace Waggo.Infrastructure.Options.Identity;

/// <summary>
/// Bound from "Seed:Users" (DEV-001). Every account is optional: unset means it is not seeded. Only read in
/// Development and Testing (<see cref="Services.Identity.DevelopmentUsersSeeder"/>), so each role can sign in for real
/// instead of the development-user bypass (ADR-011). Never set it in <c>appsettings.json</c>.
/// </summary>
public sealed class SeedUsersOptions
{
    public const string SectionName = "Seed:Users";

    public SeedUserOptions? Admin { get; set; }

    public SeedUserOptions? Owner { get; set; }

    /// <summary>Also gets an approved walker profile, so its session carries the <c>walker</c> role.</summary>
    public SeedUserOptions? Walker { get; set; }
}
