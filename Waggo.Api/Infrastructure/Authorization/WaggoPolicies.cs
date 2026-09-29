namespace Waggo.Api.Infrastructure.Authorization;

/// <summary>Names of the role-based authorization policies (registered in ServiceCollectionExtensions).</summary>
public static class WaggoPolicies
{
    /// <summary>owner or admin.</summary>
    public const string Owner = "OwnerPolicy";

    /// <summary>walker or admin.</summary>
    public const string Walker = "WalkerPolicy";

    public const string Admin = "AdminPolicy";
}
