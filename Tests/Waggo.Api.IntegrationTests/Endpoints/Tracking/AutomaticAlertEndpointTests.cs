using System.Net;
using System.Net.Http.Json;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Tracking;
using Waggo.Application.Walks;

namespace Waggo.Api.IntegrationTests.Endpoints.Tracking;

/// <summary>Acceptance tests for RF-009 and RF-010: the platform raises alerts by itself from the route.</summary>
[Collection(ApiCollectionDefinition.Name)]
public class AutomaticAlertEndpointTests(WaggoApiFactory factory)
{
    [Fact]
    public async Task Track_LeavingTheZone_RaisesOneGeofenceAlertForTheOwner()
    {
        (HttpClient owner, HttpClient walker, WalkResponse walk) = await StartedWalkAsync();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        await SendAsync(walker, walk.Id, (4.6400, now), (4.6631, now.AddMinutes(3)));
        await SendAsync(walker, walk.Id, (4.6640, now.AddMinutes(4)));

        List<WalkAlertResponse> alerts = await AlertsAsync(owner, walk.Id);
        WalkAlertResponse alert = alerts.Single();
        (alert.Kind, alert.RaisedBy).ShouldBe(("Geofence", null));
        alert.Latitude.ShouldBe(4.6631);
    }

    [Fact]
    public async Task Track_StoppedForElevenMinutes_RaisesOneAnomaly()
    {
        (HttpClient owner, HttpClient walker, WalkResponse walk) = await StartedWalkAsync();
        DateTimeOffset now = DateTimeOffset.UtcNow.AddMinutes(-15);

        await SendAsync(walker, walk.Id, (4.6400, now), (4.6401, now.AddMinutes(5)), (4.6400, now.AddMinutes(11)));
        await SendAsync(walker, walk.Id, (4.6401, now.AddMinutes(13)));

        List<WalkAlertResponse> alerts = await AlertsAsync(owner, walk.Id);
        alerts.Single().Kind.ShouldBe("Anomaly");
    }

    [Fact]
    public async Task Track_NormalWalk_RaisesNothing()
    {
        (HttpClient owner, HttpClient walker, WalkResponse walk) = await StartedWalkAsync();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        await SendAsync(walker, walk.Id, (4.6361, now), (4.6400, now.AddMinutes(3)), (4.6440, now.AddMinutes(6)));

        (await AlertsAsync(owner, walk.Id)).ShouldBeEmpty();
    }

    private async Task<(HttpClient Owner, HttpClient Walker, WalkResponse Walk)> StartedWalkAsync()
    {
        HttpClient owner = WalkScenario.Client(factory, "owner");
        HttpClient walker = WalkScenario.Client(factory, "walker");
        WalkResponse walk = await WalkScenario.RequestAsync(owner);
        await WalkScenario.AcceptAsync(walker, walk.Id, start: true);
        return (owner, walker, walk);
    }

    private static async Task SendAsync(
        HttpClient walker,
        Guid walkId,
        params (double Latitude, DateTimeOffset RecordedAt)[] points)
    {
        HttpResponseMessage response = await walker.PostAsJsonAsync(WalkScenario.WalkUri(walkId, "track"), new
        {
            points = points.Select(p => new { latitude = p.Latitude, longitude = -74.0645, recordedAt = p.RecordedAt }),
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task<List<WalkAlertResponse>> AlertsAsync(HttpClient client, Guid walkId) =>
        (await client.GetFromJsonAsync<WaggoApiResponse<List<WalkAlertResponse>>>(
            WalkScenario.WalkUri(walkId, "alerts")))!.Data!;
}
