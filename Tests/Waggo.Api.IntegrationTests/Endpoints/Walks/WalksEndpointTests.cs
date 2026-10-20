using System.Net;
using System.Net.Http.Json;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Pets;
using Waggo.Application.Walks;

namespace Waggo.Api.IntegrationTests.Endpoints.Walks;

/// <summary>Acceptance tests for RF-007 (owner side): request, list, see and cancel a walk.</summary>
[Collection(ApiCollectionDefinition.Name)]
public class WalksEndpointTests(WaggoApiFactory factory)
{
    private static readonly Uri s_walks = new("/api/v1/walks", UriKind.Relative);

    [Fact]
    public async Task Post_WalkForNow_Returns200RequestedWithTheFrozenFare()
    {
        using HttpClient client = ClientForNewOwner();
        Guid luna = await RegisterPetAsync(client, "Luna");

        HttpResponseMessage response = await client.PostAsJsonAsync(s_walks, WalkRequest([luna]));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        WalkResponse walk = (await response.Content.ReadFromJsonAsync<WaggoApiResponse<WalkResponse>>())!.Data!;
        walk.Status.ShouldBe("Requested");
        walk.PetIds.ShouldBe([luna]);
        walk.WalkType.ShouldBe("Individual");
        walk.DurationMinutes.ShouldBe(60);
        walk.PickupAddress.ShouldBe("Cra 7 # 45-10, Bogotá");
        walk.Latitude.ShouldBe(4.6361, 0.000001);
        walk.Longitude.ShouldBe(-74.0645, 0.000001);
        walk.ScheduledFor.ShouldBe(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        walk.Notes.ShouldBe("Timbre dañado, llamar al llegar");
        (walk.Currency, walk.Total, walk.Commission, walk.WalkerPayout).ShouldBe(("COP", 23000m, 4600m, 18400m));
        walk.WalkerId.ShouldBeNull();
    }

    [Fact]
    public async Task Post_ScheduledGroupWalkWithTwoDogs_UsesTheGroupFare()
    {
        using HttpClient client = ClientForNewOwner();
        Guid luna = await RegisterPetAsync(client, "Luna");
        Guid max = await RegisterPetAsync(client, "Max");
        DateTimeOffset tomorrow = DateTimeOffset.UtcNow.AddDays(1);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            s_walks,
            WalkRequest([luna, max], walkType: "Group", durationMinutes: 45, scheduledFor: tomorrow));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        WalkResponse walk = (await response.Content.ReadFromJsonAsync<WaggoApiResponse<WalkResponse>>())!.Data!;
        walk.Total.ShouldBe(11800m);
        walk.PetIds.ShouldBe([luna, max], ignoreOrder: true);
        walk.ScheduledFor.ShouldBe(tomorrow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Get_ReturnsOnlyTheOwnersWalksNewestFirst()
    {
        using HttpClient owner = ClientForNewOwner();
        using HttpClient otherOwner = ClientForNewOwner();
        Guid luna = await RegisterPetAsync(owner, "Luna");
        WalkResponse first = await RequestWalkAsync(owner, WalkRequest([luna]));
        WalkResponse second = await RequestWalkAsync(owner, WalkRequest([luna], durationMinutes: 30));

        List<WalkResponse> mine = (await owner.GetFromJsonAsync<WaggoApiResponse<List<WalkResponse>>>(s_walks))!.Data!;
        List<WalkResponse> others =
            (await otherOwner.GetFromJsonAsync<WaggoApiResponse<List<WalkResponse>>>(s_walks))!.Data!;

        mine.Select(walk => walk.Id).ShouldBe([second.Id, first.Id]);
        others.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetById_WalkOfAnotherOwner_Returns404()
    {
        using HttpClient owner = ClientForNewOwner();
        using HttpClient otherOwner = ClientForNewOwner();
        WalkResponse walk = await RequestWalkAsync(owner, WalkRequest([await RegisterPetAsync(owner, "Luna")]));

        HttpResponseMessage response = await otherOwner.GetAsync(WalkUri(walk.Id));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        await ShouldHaveSingleErrorAsync(response, "Walks.NotFound");
    }

    [Fact]
    public async Task GetById_OwnWalk_ReturnsIt()
    {
        using HttpClient client = ClientForNewOwner();
        WalkResponse walk = await RequestWalkAsync(client, WalkRequest([await RegisterPetAsync(client, "Luna")]));

        WaggoApiResponse<WalkResponse>? body =
            await client.GetFromJsonAsync<WaggoApiResponse<WalkResponse>>(WalkUri(walk.Id));

        body!.Data.ShouldNotBeNull();
        body.Data.Id.ShouldBe(walk.Id);
        body.Data.Status.ShouldBe("Requested");
    }

    [Fact]
    public async Task Cancel_RequestedWalk_ReturnsCancelled_AndTwiceReturns422()
    {
        using HttpClient client = ClientForNewOwner();
        WalkResponse walk = await RequestWalkAsync(client, WalkRequest([await RegisterPetAsync(client, "Luna")]));

        HttpResponseMessage first = await client.PostAsync(CancelUri(walk.Id), content: null);
        HttpResponseMessage second = await client.PostAsync(CancelUri(walk.Id), content: null);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await first.Content.ReadFromJsonAsync<WaggoApiResponse<WalkResponse>>())!.Data!.Status.ShouldBe("Cancelled");
        second.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        await ShouldHaveSingleErrorAsync(second, "Walks.CannotCancel");
    }

    [Fact]
    public async Task Post_PetOfAnotherOwner_Returns400InvalidPets()
    {
        using HttpClient owner = ClientForNewOwner();
        using HttpClient otherOwner = ClientForNewOwner();
        Guid foreignPet = await RegisterPetAsync(otherOwner, "Toby");

        HttpResponseMessage response = await owner.PostAsJsonAsync(s_walks, WalkRequest([foreignPet]));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await ShouldHaveSingleErrorAsync(response, "Walks.InvalidPets");
    }

    [Fact]
    public async Task Post_InvalidDuration_Returns400WithThePricingCode()
    {
        using HttpClient client = ClientForNewOwner();
        Guid luna = await RegisterPetAsync(client, "Luna");

        HttpResponseMessage response = await client.PostAsJsonAsync(s_walks, WalkRequest([luna], durationMinutes: 50));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await ShouldHaveSingleErrorAsync(response, "Pricing.InvalidDuration");
    }

    [Fact]
    public async Task Post_ScheduledTooFarAhead_Returns400InvalidSchedule()
    {
        using HttpClient client = ClientForNewOwner();
        Guid luna = await RegisterPetAsync(client, "Luna");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            s_walks,
            WalkRequest([luna], scheduledFor: DateTimeOffset.UtcNow.AddDays(15)));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await ShouldHaveSingleErrorAsync(response, "Walks.InvalidSchedule");
    }

    [Fact]
    public async Task Post_WithoutAddress_FailsTheDtoValidation()
    {
        using HttpClient client = ClientForNewOwner();
        Guid luna = await RegisterPetAsync(client, "Luna");

        HttpResponseMessage response = await client.PostAsJsonAsync(s_walks, WalkRequest([luna], pickupAddress: null));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await ShouldHaveSingleErrorAsync(response, "Request.Required");
    }

    [Fact]
    public async Task Post_AsWalker_Returns403()
    {
        using HttpClient client = ClientForNewOwner();
        client.DefaultRequestHeaders.Add("X-Dev-Roles", "walker");

        HttpResponseMessage response = await client.PostAsJsonAsync(s_walks, WalkRequest([Guid.NewGuid()]));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static object WalkRequest(
        Guid[] petIds,
        string walkType = "Individual",
        int durationMinutes = 60,
        string? pickupAddress = "Cra 7 # 45-10, Bogotá",
        DateTimeOffset? scheduledFor = null) => new
        {
            petIds,
            walkType,
            durationMinutes,
            pickupAddress,
            latitude = 4.6361,
            longitude = -74.0645,
            scheduledFor,
            notes = "Timbre dañado, llamar al llegar",
        };

    private HttpClient ClientForNewOwner()
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User-Id", $"owner-{Guid.NewGuid():N}");
        return client;
    }

    private static Uri WalkUri(Guid id) => new($"/api/v1/walks/{id}", UriKind.Relative);

    private static Uri CancelUri(Guid id) => new($"/api/v1/walks/{id}/cancel", UriKind.Relative);

    private static async Task<Guid> RegisterPetAsync(HttpClient client, string name)
    {
        HttpResponseMessage response =
            await client.PostAsJsonAsync(new Uri("/api/v1/pets", UriKind.Relative), new { name, size = "Medium" });
        return (await response.Content.ReadFromJsonAsync<WaggoApiResponse<PetResponse>>())!.Data!.Id;
    }

    private static async Task<WalkResponse> RequestWalkAsync(HttpClient client, object request)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(s_walks, request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<WaggoApiResponse<WalkResponse>>())!.Data!;
    }

    private static async Task ShouldHaveSingleErrorAsync(HttpResponseMessage response, string code)
    {
        WaggoApiResponse<object>? body = await response.Content.ReadFromJsonAsync<WaggoApiResponse<object>>();
        body!.Success.ShouldBeFalse();
        body.Errors.Single().Code.ShouldBe(code);
    }
}
