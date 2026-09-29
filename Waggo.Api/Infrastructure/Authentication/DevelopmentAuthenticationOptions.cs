using Microsoft.AspNetCore.Authentication;
using Waggo.Api.Infrastructure.Settings;

namespace Waggo.Api.Infrastructure.Authentication;

/// <summary>Options of <see cref="DevelopmentAuthenticationHandler"/>.</summary>
internal sealed class DevelopmentAuthenticationOptions : AuthenticationSchemeOptions
{
    public DevelopmentUserSettings User { get; set; } = new();
}
