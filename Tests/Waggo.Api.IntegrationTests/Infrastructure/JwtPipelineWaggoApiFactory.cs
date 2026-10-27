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

        // DEV-001: the same test accounts as appsettings.Development.json, so each role signs in for real.
        builder.UseSetting("Seed:Users:Admin:Email", "admin@waggo.local");
        builder.UseSetting("Seed:Users:Admin:Password", "Admin1234");
        builder.UseSetting("Seed:Users:Owner:Email", "dueno@waggo.local");
        builder.UseSetting("Seed:Users:Owner:Password", "Dueno1234");
        builder.UseSetting("Seed:Users:Walker:Email", "paseador@waggo.local");
        builder.UseSetting("Seed:Users:Walker:Password", "Paseador1234");
        builder.UseSetting("Seed:Users:Walker:FullName", "Carlos Ramírez");
        builder.UseSetting("Seed:Users:Walker:Phone", "3000000003");
        builder.UseSetting("Seed:Users:Walker:DocumentNumber", "1000000003");
    }
}
