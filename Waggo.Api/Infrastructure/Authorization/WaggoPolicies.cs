namespace Waggo.Api.Infrastructure.Authorization;

/// <summary>Names of the authorization policies (registered in ServiceCollectionExtensions).</summary>
public static class WaggoPolicies
{
    /// <summary>Any signed-in user, whatever their roles (e.g. someone becoming a walker, RF-002).</summary>
    public const string Authenticated = "AuthenticatedPolicy";

    /// <summary>owner or admin.</summary>
    public const string Owner = "OwnerPolicy";

    /// <summary>walker or admin.</summary>
    public const string Walker = "WalkerPolicy";

    public const string Admin = "AdminPolicy";
}
