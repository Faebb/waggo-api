using System.Net;
using System.Net.Http.Json;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Pets;
using Waggo.Application.Walks;

namespace Waggo.Api.IntegrationTests.Endpoints.Walks;

/// <summary>Acceptance tests for RF-007 (walker side): see open requests, nearest first, and accept one.</summary>
[Collection(ApiCollectionDefinition.Name)]
public class WalkerEndpointTests(WaggoApiFactory factory)
{
    private static readonly Uri s_walks = new("/api/v1/walks", UriKind.Relative);
    private static readonly Uri s_assigned = new("/api/v1/walks/assigned", UriKind.Relative);

    [Fact]
    public async Task Available_ShowsOpenRequestsWithThePayout()
    {
        using HttpClient owner = Client("owner");
        using HttpClient walker = Client("walker");
        WalkResponse walk = await RequestWalkAsync(owner, 4.6361, -74.0645);

        AvailableWalkResponse offer = (await AvailableAsync(walker)).Single(w => w.Id == walk.Id);

        (offer.WalkType, offer.DurationMinutes, offer.PetCount).ShouldBe(("Individual", 60, 1));
        (offer.Currency, offer.WalkerPayout).ShouldBe(("COP", 18400m));
        offer.PickupAddress.ShouldBe("Cra 7 # 45-10, Bogotá");
        offer.DistanceKm.ShouldBeNull();
    }

    [Fact]
    public async Task Available_WithLocation_ListsTheNearestFirst()
    {
        using HttpClient owner = Client("owner");
        using HttpClient walker = Client("walker");
        WalkResponse far = await RequestWalkAsync(owner, 4.6800, -74.0600);  // ~5 km north
        WalkResponse near = await RequestWalkAsync(owner, 4.6450, -74.0600); // ~1 km

        List<AvailableWalkResponse> offers = await AvailableAsync(walker, "?latitude=4.6361&longitude=-74.0645");

        List<AvailableWalkResponse> mine = [.. offers.Where(w => w.Id == far.Id || w.Id == near.Id)];
        mine.Select(w => w.Id).ShouldBe([near.Id, far.Id]);
        mine[0].DistanceKm!.Value.ShouldBe(1.1, 0.2);
        mine[1].DistanceKm!.Value.ShouldBe(4.9, 0.3);
    }

    [Fact]
    public async Task Available_WithLocation_LeavesOutRequestsFartherThan5Km_RF006()
    {
        using HttpClient owner = Client("owner");
        using HttpClient walker = Client("walker");
        WalkResponse justOutside = await RequestWalkAsync(owner, 4.6830, -74.0645); // ~5.2 km
        WalkResponse far = await RequestWalkAsync(owner, 4.7081, -74.0645);        // ~8 km
        WalkResponse near = await RequestWalkAsync(owner, 4.6450, -74.0645);       // ~1 km

        List<AvailableWalkResponse> offers = await AvailableAsync(walker, "?latitude=4.6361&longitude=-74.0645");
        List<AvailableWalkResponse> all = await AvailableAsync(walker);

        offers.ShouldContain(w => w.Id == near.Id);
        offers.ShouldNotContain(w => w.Id == justOutside.Id);
        offers.ShouldNotContain(w => w.Id == far.Id);
        offers.ShouldAllBe(w => w.DistanceKm <= 5);
        offers.Select(w => w.DistanceKm).ShouldBeInOrder(SortDirection.Ascending);
        all.ShouldContain(w => w.Id == far.Id);
    }

    [Fact]
    public async Task Available_HidesCancelledWalksAndTheWalkersOwnWalks()
    {
        string ownerId = NewId("owner");
        using HttpClient owner = Client("owner", ownerId);
        using HttpClient ownerAsWalker = Client("owner,walker", ownerId);
        WalkResponse cancelled = await RequestWalkAsync(owner, 4.6361, -74.0645);
        await owner.PostAsync(new Uri($"/api/v1/walks/{cancelled.Id}/cancel", UriKind.Relative), content: null);
        WalkResponse own = await RequestWalkAsync(owner, 4.6361, -74.0645);

        List<AvailableWalkResponse> offers = await AvailableAsync(ownerAsWalker);

        offers.ShouldNotContain(w => w.Id == cancelled.Id);
        offers.ShouldNotContain(w => w.Id == own.Id);
    }

    [Fact]
    public async Task Accept_OpenRequest_AssignsTheWalker_AndTheOwnerSeesIt()
    {
        using HttpClient owner = Client("owner");
        string walkerId = NewId("walker");
        using HttpClient walker = Client("walker", walkerId);
        WalkResponse walk = await RequestWalkAsync(owner, 4.6361, -74.0645);

        HttpResponseMessage response = await walker.PostAsync(AcceptUri(walk.Id), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        WalkResponse accepted = (await response.Content.ReadFromJsonAsync<WaggoApiResponse<WalkResponse>>())!.Data!;
        (accepted.Status, accepted.WalkerId).ShouldBe(("Accepted", walkerId));

        WalkResponse seenByOwner =
            (await owner.GetFromJsonAsync<WaggoApiResponse<WalkResponse>>(WalkUri(walk.Id)))!.Data!;
        (seenByOwner.Status, seenByOwner.WalkerId).ShouldBe(("Accepted", walkerId));
    }

    [Fact]
    public async Task Accept_AlreadyTaken_Returns409NotAvailable()
    {
        using HttpClient owner = Client("owner");
        using HttpClient first = Client("walker");
        using HttpClient second = Client("walker");
        WalkResponse walk = await RequestWalkAsync(owner, 4.6361, -74.0645);
        await first.PostAsync(AcceptUri(walk.Id), content: null);

        HttpResponseMessage response = await second.PostAsync(AcceptUri(walk.Id), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        await ShouldHaveSingleErrorAsync(response, "Walks.NotAvailable");
    }

    [Fact]
    public async Task Accept_OwnWalk_Returns422()
    {
        string ownerId = NewId("owner");
        using HttpClient owner = Client("owner", ownerId);
        using HttpClient ownerAsWalker = Client("owner,walker", ownerId);
        WalkResponse walk = await RequestWalkAsync(owner, 4.6361, -74.0645);

        HttpResponseMessage response = await ownerAsWalker.PostAsync(AcceptUri(walk.Id), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        await ShouldHaveSingleErrorAsync(response, "Walks.OwnWalk");
    }

    [Fact]
    public async Task Accept_UnknownWalk_Returns404()
    {
        using HttpClient walker = Client("walker");

        HttpResponseMessage response = await walker.PostAsync(AcceptUri(Guid.NewGuid()), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        await ShouldHaveSingleErrorAsync(response, "Walks.NotFound");
    }

    [Fact]
    public async Task Assigned_ListsTheWalksTheWalkerAccepted_AndTheWalkerSeesTheDetail()
    {
        using HttpClient owner = Client("owner");
        using HttpClient walker = Client("walker");
        using HttpClient otherWalker = Client("walker");
        WalkResponse walk = await RequestWalkAsync(owner, 4.6361, -74.0645);
        await walker.PostAsync(AcceptUri(walk.Id), content: null);

        List<WalkResponse> assigned = (await walker.GetFromJsonAsync<WaggoApiResponse<List<WalkResponse>>>(s_assigned))!.Data!;
        HttpResponseMessage detail = await walker.GetAsync(WalkUri(walk.Id));
        HttpResponseMessage foreignDetail = await otherWalker.GetAsync(WalkUri(walk.Id));

        assigned.Select(w => w.Id).ShouldBe([walk.Id]);
        detail.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await detail.Content.ReadFromJsonAsync<WaggoApiResponse<WalkResponse>>())!.Data!.Notes.ShouldBe("Timbre dañado");
        foreignDetail.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Available_AsOwnerOnly_Returns403()
    {
        using HttpClient owner = Client("owner");

        HttpResponseMessage response = await owner.GetAsync(new Uri("/api/v1/walks/available", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Available_InvalidLocation_Returns400()
    {
        using HttpClient walker = Client("walker");

        HttpResponseMessage response = await walker.GetAsync(
            new Uri("/api/v1/walks/available?latitude=95&longitude=0", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await ShouldHaveSingleErrorAsync(response, "Walks.InvalidLocation");
    }

    private static string NewId(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    /// <summary>A client signed in as a new user with the given roles (comma separated).</summary>
    private HttpClient Client(string roles, string? userId = null)
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User-Id", userId ?? NewId(roles.Split(',')[0]));
        client.DefaultRequestHeaders.Add("X-Dev-Roles", roles);
        return client;
    }

    private static Uri WalkUri(Guid id) => new($"/api/v1/walks/{id}", UriKind.Relative);

    private static Uri AcceptUri(Guid id) => new($"/api/v1/walks/{id}/accept", UriKind.Relative);

    private static async Task<List<AvailableWalkResponse>> AvailableAsync(HttpClient walker, string query = "")
    {
        HttpResponseMessage response = await walker.GetAsync(new Uri($"/api/v1/walks/available{query}", UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<WaggoApiResponse<List<AvailableWalkResponse>>>())!.Data!;
    }

    private static async Task<WalkResponse> RequestWalkAsync(HttpClient owner, double latitude, double longitude)
    {
        HttpResponseMessage pet = await owner.PostAsJsonAsync(
            new Uri("/api/v1/pets", UriKind.Relative),
            new { name = "Luna", size = "Medium" });
        Guid petId = (await pet.Content.ReadFromJsonAsync<WaggoApiResponse<PetResponse>>())!.Data!.Id;

        HttpResponseMessage response = await owner.PostAsJsonAsync(s_walks, new
        {
            petIds = new[] { petId },
            walkType = "Individual",
            durationMinutes = 60,
            pickupAddress = "Cra 7 # 45-10, Bogotá",
            latitude,
            longitude,
            notes = "Timbre dañado",
        });
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
