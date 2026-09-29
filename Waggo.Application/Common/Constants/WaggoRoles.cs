namespace Waggo.Application.Common.Constants;

/// <summary>Roles of the platform (value of the role claim in the access token).</summary>
public static class WaggoRoles
{
    public const string Owner = "owner";
    public const string Walker = "walker";
    public const string Admin = "admin";

    public static readonly IReadOnlyList<string> All = [Owner, Walker, Admin];
}
