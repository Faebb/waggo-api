using System.Net;
using System.Net.Http.Json;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Payments;
using Waggo.Application.Walks;

namespace Waggo.Api.IntegrationTests.Endpoints.Payments;

/// <summary>Acceptance tests for RF-015 – RF-018: the fare is held on request, released or captured and split.</summary>
[Collection(ApiCollectionDefinition.Name)]
public class PaymentEndpointTests(WaggoApiFactory factory)
{
    private static readonly Uri s_earnings = new("/api/v1/payments/earnings", UriKind.Relative);

    [Fact]
    public async Task RequestWalk_HoldsTheFrozenFare()
    {
        HttpClient owner = WalkScenario.Client(factory, "owner");
        WalkResponse walk = await WalkScenario.RequestAsync(owner);

        WalkPaymentResponse payment = await PaymentAsync(owner, walk.Id);

        payment.Status.ShouldBe("Held");
        (payment.Total, payment.Commission, payment.WalkerPayout, payment.Currency)
            .ShouldBe((23000m, 4600m, 18400m, "COP"));
    }

    [Fact]
    public async Task RequestWalk_DeclinedCard_FailsAndCreatesNoWalk()
    {
        HttpClient owner = WalkScenario.Client(factory, "owner");
        owner.DefaultRequestHeaders.Remove("X-Dev-User-Id");
        owner.DefaultRequestHeaders.Add("X-Dev-User-Id", $"owner-card-declined-{Guid.NewGuid():N}");
        HttpResponseMessage pet = await owner.PostAsJsonAsync(
            new Uri("/api/v1/pets", UriKind.Relative),
            new { name = "Luna", size = "Medium" });
        pet.StatusCode.ShouldBe(HttpStatusCode.OK);
        Guid petId = (await pet.Content.ReadFromJsonAsync<WaggoApiResponse<Waggo.Application.Pets.PetResponse>>())!
            .Data!.Id;

        HttpResponseMessage response = await owner.PostAsJsonAsync(new Uri("/api/v1/walks", UriKind.Relative), new
        {
            petIds = new[] { petId },
            walkType = "Individual",
            durationMinutes = 60,
            pickupAddress = "Cra 7 # 45-10, Bogotá",
            latitude = 4.6361,
            longitude = -74.0645,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        await WalkScenario.ShouldHaveSingleErrorAsync(response, "Payments.Declined");
        List<WalkResponse> mine = (await owner.GetFromJsonAsync<WaggoApiResponse<List<WalkResponse>>>(
            new Uri("/api/v1/walks", UriKind.Relative)))!.Data!;
        mine.ShouldBeEmpty();
    }

    [Fact]
    public async Task CancelWalk_ReleasesTheHold()
    {
        HttpClient owner = WalkScenario.Client(factory, "owner");
        WalkResponse walk = await WalkScenario.RequestAsync(owner);

        (await owner.PostAsync(WalkScenario.WalkUri(walk.Id, "cancel"), content: null))
            .StatusCode.ShouldBe(HttpStatusCode.OK);

        (await PaymentAsync(owner, walk.Id)).Status.ShouldBe("Released");
    }

    [Fact]
    public async Task FinishWalk_CapturesAndTheWalkerSeesTheEarnings()
    {
        HttpClient owner = WalkScenario.Client(factory, "owner");
        HttpClient walker = await WalkScenario.VerifiedWalkerAsync(factory);
        WalkResponse walk = await WalkScenario.RequestAsync(owner);
        await WalkScenario.AcceptAsync(walker, walk.Id, start: true);

        (await walker.PostAsync(WalkScenario.WalkUri(walk.Id, "finish"), content: null))
            .StatusCode.ShouldBe(HttpStatusCode.OK);

        WalkPaymentResponse payment = await PaymentAsync(walker, walk.Id);
        payment.Status.ShouldBe("Captured");
        payment.CapturedAt.ShouldNotBeNull();

        WalkerEarningsResponse earnings =
            (await walker.GetFromJsonAsync<WaggoApiResponse<WalkerEarningsResponse>>(s_earnings))!.Data!;
        (earnings.Total, earnings.Currency).ShouldBe((18400m, "COP"));
        earnings.Walks.Single().WalkId.ShouldBe(walk.Id);
    }

    [Fact]
    public async Task Earnings_NoFinishedWalks_IsEmpty()
    {
        HttpClient walker = WalkScenario.Client(factory, "walker");

        WalkerEarningsResponse earnings =
            (await walker.GetFromJsonAsync<WaggoApiResponse<WalkerEarningsResponse>>(s_earnings))!.Data!;

        earnings.Total.ShouldBe(0m);
        earnings.Walks.ShouldBeEmpty();
    }

    [Fact]
    public async Task Payment_Stranger_GetsNotFound()
    {
        HttpClient owner = WalkScenario.Client(factory, "owner");
        WalkResponse walk = await WalkScenario.RequestAsync(owner);
        HttpClient stranger = WalkScenario.Client(factory, "owner");

        HttpResponseMessage response = await stranger.GetAsync(WalkScenario.WalkUri(walk.Id, "payment"));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        await WalkScenario.ShouldHaveSingleErrorAsync(response, "Walks.NotFound");
    }

    private static async Task<WalkPaymentResponse> PaymentAsync(HttpClient client, Guid walkId)
    {
        HttpResponseMessage response = await client.GetAsync(WalkScenario.WalkUri(walkId, "payment"));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<WaggoApiResponse<WalkPaymentResponse>>())!.Data!;
    }
}
