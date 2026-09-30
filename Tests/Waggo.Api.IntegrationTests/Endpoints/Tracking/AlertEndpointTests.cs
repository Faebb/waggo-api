using System.Net;
using System.Net.Http.Json;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Tracking;
using Waggo.Application.Walks;

namespace Waggo.Api.IntegrationTests.Endpoints.Tracking;

/// <summary>Acceptance tests for RF-012: the owner or the walker raises an emergency during the walk.</summary>
[Collection(ApiCollectionDefinition.Name)]
public class AlertEndpointTests(WaggoApiFactory factory)
{
    [Fact]
    public async Task Emergency_ByTheWalkerDuringTheWalk_IsRecordedAndTheOwnerSeesIt()
    {
        using HttpClient owner = WalkScenario.Client(factory, "owner");
        using HttpClient walker = WalkScenario.Client(factory, "walker");
        WalkResponse walk = await WalkScenario.RequestAsync(owner);
        await WalkScenario.AcceptAsync(walker, walk.Id, start: true);

        HttpResponseMessage raised = await walker.PostAsJsonAsync(
            WalkScenario.WalkUri(walk.Id, "emergency"),
            new { message = "Luna se soltó", latitude = 4.6400, longitude = -74.0620 });
        List<WalkAlertResponse> seenByOwner = (await owner.GetFromJsonAsync<WaggoApiResponse<List<WalkAlertResponse>>>(
            WalkScenario.WalkUri(walk.Id, "alerts")))!.Data!;

        raised.StatusCode.ShouldBe(HttpStatusCode.OK);
        WalkAlertResponse alert = seenByOwner.Single();
        (alert.Kind, alert.RaisedBy, alert.Message).ShouldBe(("Emergency", "Walker", "Luna se soltó"));
        alert.Latitude.ShouldBe(4.64);
        alert.RaisedAt.ShouldBe(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Emergency_ByTheOwnerOfAnAcceptedWalk_WithoutDetails_IsRecorded()
    {
        using HttpClient owner = WalkScenario.Client(factory, "owner");
        using HttpClient walker = WalkScenario.Client(factory, "walker");
        WalkResponse walk = await WalkScenario.RequestAsync(owner);
        await WalkScenario.AcceptAsync(walker, walk.Id);

        HttpResponseMessage raised = await owner.PostAsJsonAsync(WalkScenario.WalkUri(walk.Id, "emergency"), new { });

        raised.StatusCode.ShouldBe(HttpStatusCode.OK);
        WalkAlertResponse alert = (await raised.Content.ReadFromJsonAsync<WaggoApiResponse<WalkAlertResponse>>())!.Data!;
        (alert.RaisedBy, alert.Message, alert.Latitude).ShouldBe(("Owner", null, null));
    }

    [Fact]
    public async Task Alerts_AreListedNewestFirst()
    {
        using HttpClient owner = WalkScenario.Client(factory, "owner");
        using HttpClient walker = WalkScenario.Client(factory, "walker");
        WalkResponse walk = await WalkScenario.RequestAsync(owner);
        await WalkScenario.AcceptAsync(walker, walk.Id, start: true);
        await walker.PostAsJsonAsync(WalkScenario.WalkUri(walk.Id, "emergency"), new { message = "primera" });
        await owner.PostAsJsonAsync(WalkScenario.WalkUri(walk.Id, "emergency"), new { message = "segunda" });

        List<WalkAlertResponse> alerts = (await walker.GetFromJsonAsync<WaggoApiResponse<List<WalkAlertResponse>>>(
            WalkScenario.WalkUri(walk.Id, "alerts")))!.Data!;

        alerts.Select(alert => alert.Message).ShouldBe(["segunda", "primera"]);
    }

    [Fact]
    public async Task Emergency_ByAStranger_Returns404()
    {
        using HttpClient owner = WalkScenario.Client(factory, "owner");
        using HttpClient stranger = WalkScenario.Client(factory, "owner,walker");
        WalkResponse walk = await WalkScenario.RequestAsync(owner);

        HttpResponseMessage raised = await stranger.PostAsJsonAsync(WalkScenario.WalkUri(walk.Id, "emergency"), new { });
        HttpResponseMessage listed = await stranger.GetAsync(WalkScenario.WalkUri(walk.Id, "alerts"));

        raised.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        listed.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Emergency_OnAWalkNobodyAcceptedYet_Returns422()
    {
        using HttpClient owner = WalkScenario.Client(factory, "owner");
        WalkResponse walk = await WalkScenario.RequestAsync(owner);

        HttpResponseMessage raised = await owner.PostAsJsonAsync(WalkScenario.WalkUri(walk.Id, "emergency"), new { });

        raised.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        await WalkScenario.ShouldHaveSingleErrorAsync(raised, "Alerts.WalkNotActive");
    }

    [Fact]
    public async Task Emergency_MessageTooLong_Returns400()
    {
        using HttpClient owner = WalkScenario.Client(factory, "owner");
        using HttpClient walker = WalkScenario.Client(factory, "walker");
        WalkResponse walk = await WalkScenario.RequestAsync(owner);
        await WalkScenario.AcceptAsync(walker, walk.Id);

        HttpResponseMessage raised = await owner.PostAsJsonAsync(
            WalkScenario.WalkUri(walk.Id, "emergency"),
            new { message = new string('a', 501) });

        raised.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await WalkScenario.ShouldHaveSingleErrorAsync(raised, "Alerts.InvalidMessage");
    }
}
