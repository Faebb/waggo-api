using System.Net;
using System.Net.Http.Json;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.IntegrationTests.Infrastructure;

namespace Waggo.Api.IntegrationTests.Responses;

/// <summary>Every response of the API is a WaggoApiResponse, even the ones no endpoint produces.</summary>
[Collection(ApiCollection.Name)]
public class EnvelopeTests(WaggoApiFactory factory)
{
    [Fact]
    public async Task UnknownRoute_Returns404Envelope()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/does-not-exist", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        WaggoApiResponse<object>? body = await response.Content.ReadFromJsonAsync<WaggoApiResponse<object>>();
        body!.Success.ShouldBeFalse();
        body.Errors.Single().Code.ShouldBe("Http.NotFound");
    }
}
