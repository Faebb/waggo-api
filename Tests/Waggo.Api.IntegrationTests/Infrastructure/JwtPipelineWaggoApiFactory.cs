using Microsoft.AspNetCore.Hosting;

namespace Waggo.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Only for <c>JwtPipelineTests</c>: turns off the development user so a real access token (RF-005) is what
/// authenticates every other request, exercising <c>AddWaggoAuthentication</c>'s real branch end to end.
/// </summary>
public sealed class JwtPipelineWaggoApiFactory : WaggoApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Authentication:UseDevelopmentUser", "false");
    }
}
