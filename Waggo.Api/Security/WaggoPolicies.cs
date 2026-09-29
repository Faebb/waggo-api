using Microsoft.AspNetCore.Authorization;
using Waggo.Application.Common.Security;

namespace Waggo.Api.Security;

/// <summary>Role-based authorization policies. Admin passes every policy.</summary>
public static class WaggoPolicies
{
    public const string Owner = "OwnerPolicy";
    public const string Walker = "WalkerPolicy";
    public const string Admin = "AdminPolicy";

    internal static IServiceCollection AddWaggoAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(Owner, policy => policy.RequireRole(WaggoRoles.Owner, WaggoRoles.Admin))
            .AddPolicy(Walker, policy => policy.RequireRole(WaggoRoles.Walker, WaggoRoles.Admin))
            .AddPolicy(Admin, policy => policy.RequireRole(WaggoRoles.Admin))
            // Secure by default: every endpoint needs an authenticated user unless it says AllowAnonymous.
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }
}
