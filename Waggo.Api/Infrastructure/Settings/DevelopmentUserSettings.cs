namespace Waggo.Api.Infrastructure.Settings;

/// <summary>Fake user used when Authentication:UseDevelopmentUser is enabled.</summary>
public sealed class DevelopmentUserSettings
{
    public string Id { get; set; } = "dev-user";

    public string Name { get; set; } = "Developer";

#pragma warning disable CA1819 // Bound from configuration
    public string[] Roles { get; set; } = [];
#pragma warning restore CA1819
}
