using System.Net;
using Waggo.Api.IntegrationTests.Infrastructure;

namespace Waggo.Api.IntegrationTests.Health;

[Collection(ApiCollectionDefinition.Name)]
public class HealthEndpointTests(WaggoApiFactory factory)
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Get_Health_ReturnsHealthy_WhenDatabaseIsReachable(string path)
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri(path, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }
}
