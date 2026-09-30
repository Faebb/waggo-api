using System.Net;
using System.Net.Http.Json;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Tracking;
using Waggo.Application.Walks;

namespace Waggo.Api.IntegrationTests.Endpoints.Tracking;

/// <summary>Acceptance tests for RF-008 and RF-011: start, track and finish a walk; the owner follows it live.</summary>
[Collection(ApiCollectionDefinition.Name)]
public class TrackingEndpointTests(WaggoApiFactory factory)
{
    [Fact]
    public async Task Start_AcceptedWalkByItsWalker_MovesToInProgress()
    {
        (_, HttpClient walker, WalkResponse walk) = await AcceptedWalkAsync();

        HttpResponseMessage response = await walker.PostAsync(Uri(walk.Id, "start"), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        WalkResponse started = (await response.Content.ReadFromJsonAsync<WaggoApiResponse<WalkResponse>>())!.Data!;
        started.Status.ShouldBe("InProgress");
        started.StartedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Start_ByAnotherWalker_Returns404()
    {
        (_, _, WalkResponse walk) = await AcceptedWalkAsync();
        using HttpClient stranger = Client("walker");

        HttpResponseMessage response = await stranger.PostAsync(Uri(walk.Id, "start"), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Start_Twice_Returns422CannotStart()
    {
        (_, HttpClient walker, WalkResponse walk) = await AcceptedWalkAsync();
        await walker.PostAsync(Uri(walk.Id, "start"), content: null);

        HttpResponseMessage response = await walker.PostAsync(Uri(walk.Id, "start"), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        await ShouldHaveSingleErrorAsync(response, "Walks.CannotStart");
    }

    [Fact]
    public async Task Track_WhileInProgress_TheOwnerSeesTheRouteWithItsMetrics()
    {
        (HttpClient owner, HttpClient walker, WalkResponse walk) = await AcceptedWalkAsync();
        await walker.PostAsync(Uri(walk.Id, "start"), content: null);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Sent out of order on purpose: the route is sorted by the time each position was recorded.
        HttpResponseMessage sent = await walker.PostAsJsonAsync(Uri(walk.Id, "track"), new
        {
            points = new[]
            {
                new { latitude = 4.6450, longitude = -74.0645, recordedAt = now.AddMinutes(2) },
                new { latitude = 4.6361, longitude = -74.0645, recordedAt = now },
                new { latitude = 4.6405, longitude = -74.0645, recordedAt = now.AddMinutes(1) },
            },
        });
        RouteResponse route = (await owner.GetFromJsonAsync<WaggoApiResponse<RouteResponse>>(Uri(walk.Id, "track")))!.Data!;

        sent.StatusCode.ShouldBe(HttpStatusCode.OK);
        route.Points.Select(point => point.Latitude).ShouldBe([4.6361, 4.6405, 4.6450]);
        route.DistanceKm.ShouldBe(0.99, 0.02);
        route.ElapsedMinutes.ShouldBeInRange(0, 1);
    }

    [Fact]
    public async Task Track_BeforeStarting_Returns422()
    {
        (_, HttpClient walker, WalkResponse walk) = await AcceptedWalkAsync();

        HttpResponseMessage response = await walker.PostAsJsonAsync(Uri(walk.Id, "track"), OnePoint());

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        await ShouldHaveSingleErrorAsync(response, "Tracking.WalkNotInProgress");
    }

    [Fact]
    public async Task Track_EmptyBatch_Returns400()
    {
        (_, HttpClient walker, WalkResponse walk) = await AcceptedWalkAsync();
        await walker.PostAsync(Uri(walk.Id, "start"), content: null);

        HttpResponseMessage response = await walker.PostAsJsonAsync(Uri(walk.Id, "track"), new { points = Array.Empty<object>() });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await ShouldHaveSingleErrorAsync(response, "Tracking.InvalidBatch");
    }

    [Fact]
    public async Task Route_OfAnotherOwner_Returns404()
    {
        (_, _, WalkResponse walk) = await AcceptedWalkAsync();
        using HttpClient stranger = Client("owner");

        HttpResponseMessage response = await stranger.GetAsync(Uri(walk.Id, "track"));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Finish_InProgressWalk_CompletesIt_AndRejectsNewPositions()
    {
        (_, HttpClient walker, WalkResponse walk) = await AcceptedWalkAsync();
        await walker.PostAsync(Uri(walk.Id, "start"), content: null);

        HttpResponseMessage finished = await walker.PostAsync(Uri(walk.Id, "finish"), content: null);
        HttpResponseMessage lateTrack = await walker.PostAsJsonAsync(Uri(walk.Id, "track"), OnePoint());
        HttpResponseMessage finishAgain = await walker.PostAsync(Uri(walk.Id, "finish"), content: null);

        finished.StatusCode.ShouldBe(HttpStatusCode.OK);
        WalkResponse completed = (await finished.Content.ReadFromJsonAsync<WaggoApiResponse<WalkResponse>>())!.Data!;
        completed.Status.ShouldBe("Completed");
        completed.FinishedAt.ShouldNotBeNull();
        lateTrack.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        finishAgain.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        await ShouldHaveSingleErrorAsync(finishAgain, "Walks.CannotFinish");
    }

    private static object OnePoint() =>
        new { points = new[] { new { latitude = 4.6361, longitude = -74.0645, recordedAt = DateTimeOffset.UtcNow } } };

    private HttpClient Client(string roles) => WalkScenario.Client(factory, roles);

    private static Uri Uri(Guid walkId, string action) => WalkScenario.WalkUri(walkId, action);

    /// <summary>An owner requests a walk and a walker accepts it.</summary>
    private async Task<(HttpClient Owner, HttpClient Walker, WalkResponse Walk)> AcceptedWalkAsync()
    {
        HttpClient owner = Client("owner");
        HttpClient walker = Client("walker");
        WalkResponse walk = await WalkScenario.RequestAsync(owner);
        await WalkScenario.AcceptAsync(walker, walk.Id);
        return (owner, walker, walk);
    }

    private static Task ShouldHaveSingleErrorAsync(HttpResponseMessage response, string code) =>
        WalkScenario.ShouldHaveSingleErrorAsync(response, code);
}
