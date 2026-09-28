using System.Net;
using Waggo.Api.IntegrationTests.Infrastructure;

namespace Waggo.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class HealthEndpointTests(WaggoApiFactory factory)
{
    [Fact]
    public async Task Get_Health_ReturnsHealthy_WhenDatabaseIsReachable()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }
}
