using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Pricing.QuoteFare;

namespace Waggo.Api.IntegrationTests.Pricing;

/// <summary>Acceptance test for RF-019: the owner sees the price before confirming.</summary>
[Collection(ApiCollection.Name)]
public class FareQuoteEndpointTests(WaggoApiFactory factory)
{
    private static Uri Quote(string walkType, int minutes) =>
        new($"/api/v1/pricing/quote?walkType={walkType}&durationMinutes={minutes}", UriKind.Relative);

    [Fact]
    public async Task Get_ValidQuote_Returns200WithBreakdown()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(Quote("Individual", 60));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<FareQuoteResponse>();
        body.ShouldBe(new FareQuoteResponse("Individual", 60, "COP", 23000m, 4600m, 18400m));
    }

    [Fact]
    public async Task Get_InvalidDuration_Returns400ProblemWithErrorCode()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(Quote("Individual", 20));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString().ShouldBe("Pricing.InvalidDuration");
    }

    [Fact]
    public async Task Get_UnknownWalkType_Returns400()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(Quote("Skateboard", 60));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
