using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Waggo.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real API against a throw-away PostgreSQL + PostGIS container.
/// Requires Docker running on the machine (Docker Desktop on Windows).
/// </summary>
public sealed class WaggoApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgis/postgis:17-3.5")
        .WithDatabase("waggo_tests")
        .WithUsername("waggo")
        .WithPassword("waggo")
        .Build();

    public async Task InitializeAsync() => await _db.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _db.DisposeAsync();
        await DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Waggo", _db.GetConnectionString());

        // No OAuth server in tests: requests are signed in as a development user with the owner role.
        // Other roles can be tried per request with the X-Dev-Roles header.
        builder.UseSetting("Authentication:UseDevelopmentUser", "true");
        builder.UseSetting("Authentication:DevelopmentUser:Roles:0", "owner");
    }
}
