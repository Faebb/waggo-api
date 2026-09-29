using System.Net;
using System.Net.Http.Json;
using Waggo.Api.Common.Responses;
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
    public async Task Get_ValidQuote_Returns200WithEnvelope()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(Quote("Individual", 60));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        WaggoApiResponse<FareQuoteResponse>? body =
            await response.Content.ReadFromJsonAsync<WaggoApiResponse<FareQuoteResponse>>();
        body.ShouldNotBeNull();
        body.Success.ShouldBeTrue();
        body.Data.ShouldBe(new FareQuoteResponse("Individual", 60, "COP", 23000m, 4600m, 18400m));
        body.Pagination.ShouldBeNull();
        body.Errors.ShouldBeEmpty();
        body.Infos.ShouldBeEmpty(); // the "fare quoted" info is Internal: log only
        body.TraceId.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Get_InvalidDuration_Returns400EnvelopeWithErrorCode()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(Quote("Individual", 20));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        WaggoApiResponse<FareQuoteResponse>? body =
            await response.Content.ReadFromJsonAsync<WaggoApiResponse<FareQuoteResponse>>();
        body!.Success.ShouldBeFalse();
        body.Data.ShouldBeNull();
        body.Errors.Single().Code.ShouldBe("Pricing.InvalidDuration");
    }

    [Fact]
    public async Task Get_UnknownWalkType_FailsTheDtoValidation()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(Quote("Skateboard", 60));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        WaggoApiResponse<object>? body = await response.Content.ReadFromJsonAsync<WaggoApiResponse<object>>();
        body!.Errors.Single().Code.ShouldBe("Request.InvalidValue");
    }

    [Fact]
    public async Task Get_MissingWalkType_FailsTheDtoValidation()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response =
            await client.GetAsync(new Uri("/api/v1/pricing/quote?durationMinutes=60", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        WaggoApiResponse<object>? body = await response.Content.ReadFromJsonAsync<WaggoApiResponse<object>>();
        body!.Errors.Single().Code.ShouldBe("Request.Required");
    }

    [Fact]
    public async Task Get_AsWalker_Returns403Envelope()
    {
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-Roles", "walker");

        HttpResponseMessage response = await client.GetAsync(Quote("Individual", 60));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        WaggoApiResponse<object>? body = await response.Content.ReadFromJsonAsync<WaggoApiResponse<object>>();
        body!.Errors.Single().Code.ShouldBe("Auth.Forbidden");
    }
}
