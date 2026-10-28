using System.Net;
using System.Net.Http.Json;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Payments;

namespace Waggo.Api.IntegrationTests.Endpoints.Payments;

/// <summary>Acceptance tests for RF-020: the owner registers a simulated payment method (no card data stored).</summary>
[Collection(ApiCollectionDefinition.Name)]
public class PaymentMethodEndpointTests(WaggoApiFactory factory)
{
    private static readonly Uri s_methods = new("/api/v1/payments/methods", UriKind.Relative);

    [Fact]
    public async Task RegisterMethod_FirstMethod_IsTheDefault()
    {
        HttpClient owner = WalkScenario.Client(factory, "owner");

        HttpResponseMessage response = await owner.PostAsJsonAsync(
            s_methods,
            new { brand = "Visa", lastFour = "4242", expiryMonth = 12, expiryYear = 2028 });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        PaymentMethodResponse method = (await response.Content.ReadFromJsonAsync<WaggoApiResponse<PaymentMethodResponse>>())!.Data!;
        method.Brand.ShouldBe("Visa");
        method.LastFour.ShouldBe("4242");
        method.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public async Task RegisterMethod_FullCardNumber_IsRejectedAndNothingIsStored()
    {
        HttpClient owner = WalkScenario.Client(factory, "owner");

        HttpResponseMessage response = await owner.PostAsJsonAsync(
            s_methods,
            new { brand = "Visa", lastFour = "4242424242424242", expiryMonth = 12, expiryYear = 2028 });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        WaggoApiResponse<PaymentMethodResponse> body = (await response.Content.ReadFromJsonAsync<WaggoApiResponse<PaymentMethodResponse>>())!;
        body.Errors.ShouldContain(e => e.Code == "Payments.FullCardNumberNotAllowed");

        HttpResponseMessage list = await owner.GetAsync(s_methods);
        WaggoApiResponse<IReadOnlyList<PaymentMethodResponse>> listed =
            (await list.Content.ReadFromJsonAsync<WaggoApiResponse<IReadOnlyList<PaymentMethodResponse>>>())!;
        listed.Data!.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListMethods_OtherOwnersMethods_AreNotReturned()
    {
        HttpClient owner = WalkScenario.Client(factory, "owner");
        HttpClient otherOwner = WalkScenario.Client(factory, "owner");
        await otherOwner.PostAsJsonAsync(s_methods, new { brand = "Amex", lastFour = "1005", expiryMonth = 6, expiryYear = 2029 });

        HttpResponseMessage response = await owner.GetAsync(s_methods);

        WaggoApiResponse<IReadOnlyList<PaymentMethodResponse>> body =
            (await response.Content.ReadFromJsonAsync<WaggoApiResponse<IReadOnlyList<PaymentMethodResponse>>>())!;
        body.Data!.ShouldBeEmpty();
    }

    [Fact]
    public async Task RegisterMethod_AsWalker_IsForbidden()
    {
        HttpClient walker = WalkScenario.Client(factory, "walker");

        HttpResponseMessage response = await walker.PostAsJsonAsync(
            s_methods,
            new { brand = "Visa", lastFour = "4242", expiryMonth = 12, expiryYear = 2028 });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
