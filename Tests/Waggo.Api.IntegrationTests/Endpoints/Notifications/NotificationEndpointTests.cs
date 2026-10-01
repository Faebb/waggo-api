using System.Net;
using System.Net.Http.Json;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Notifications;
using Waggo.Application.Walks;

namespace Waggo.Api.IntegrationTests.Endpoints.Notifications;

/// <summary>Acceptance tests for RF-014: each moment of the walk leaves a notification for the other side.</summary>
[Collection(ApiCollectionDefinition.Name)]
public class NotificationEndpointTests(WaggoApiFactory factory)
{
    private static readonly Uri s_notifications = new("/api/v1/notifications", UriKind.Relative);

    [Fact]
    public async Task Accept_TellsTheOwner()
    {
        (HttpClient owner, _, _) = await AcceptedWalkAsync();

        NotificationsResponse inbox = await InboxAsync(owner);

        inbox.UnreadCount.ShouldBe(1);
        NotificationResponse notification = inbox.Items.Single();
        (notification.Kind, notification.Title, notification.RecipientParty)
            .ShouldBe(("WalkAccepted", "Tu paseo fue aceptado", "Owner"));
    }

    [Fact]
    public async Task StartAndFinish_TellTheOwnerAndPayTheWalker()
    {
        (HttpClient owner, HttpClient walker, WalkResponse walk) = await AcceptedWalkAsync();
        await PostOkAsync(walker, WalkScenario.WalkUri(walk.Id, "start"));
        await PostOkAsync(walker, WalkScenario.WalkUri(walk.Id, "finish"));

        (await InboxAsync(owner)).Items.Select(n => n.Kind)
            .ShouldBe(["WalkFinished", "WalkStarted", "WalkAccepted"]);
        (await InboxAsync(walker)).Items.Select(n => n.Kind).ShouldBe(["WalkPaid"]);
    }

    [Fact]
    public async Task CancelAccepted_TellsTheWalker()
    {
        (HttpClient owner, HttpClient walker, WalkResponse walk) = await AcceptedWalkAsync();

        await PostOkAsync(owner, WalkScenario.WalkUri(walk.Id, "cancel"));

        NotificationResponse notification = (await InboxAsync(walker)).Items.Single();
        (notification.Kind, notification.WalkId).ShouldBe(("WalkCancelled", walk.Id));
    }

    [Fact]
    public async Task Emergency_TellsTheOtherSideWithHighPriority()
    {
        (HttpClient owner, HttpClient walker, WalkResponse walk) = await AcceptedWalkAsync();

        HttpResponseMessage raised = await walker.PostAsJsonAsync(
            WalkScenario.WalkUri(walk.Id, "emergency"),
            new { message = "Luna se soltó" });
        raised.StatusCode.ShouldBe(HttpStatusCode.OK);

        NotificationResponse notification = (await InboxAsync(owner)).Items[0];
        (notification.Kind, notification.Priority).ShouldBe(("Emergency", "High"));
        (await InboxAsync(walker)).Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Track_LeavingTheZone_TellsTheOwner()
    {
        (HttpClient owner, HttpClient walker, WalkResponse walk) = await AcceptedWalkAsync();
        await PostOkAsync(walker, WalkScenario.WalkUri(walk.Id, "start"));
        DateTimeOffset now = DateTimeOffset.UtcNow;

        HttpResponseMessage tracked = await walker.PostAsJsonAsync(WalkScenario.WalkUri(walk.Id, "track"), new
        {
            points = new[]
            {
                new { latitude = 4.6400, longitude = -74.0645, recordedAt = now },
                new { latitude = 4.6631, longitude = -74.0645, recordedAt = now.AddMinutes(3) },
            },
        });
        tracked.StatusCode.ShouldBe(HttpStatusCode.OK);

        NotificationResponse notification = (await InboxAsync(owner)).Items[0];
        (notification.Kind, notification.Priority).ShouldBe(("Geofence", "High"));
    }

    [Fact]
    public async Task MarkRead_LeavesNothingUnread()
    {
        (HttpClient owner, _, _) = await AcceptedWalkAsync();

        HttpResponseMessage response =
            await owner.PostAsync(new Uri("/api/v1/notifications/read", UriKind.Relative), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        NotificationsResponse inbox =
            (await response.Content.ReadFromJsonAsync<WaggoApiResponse<NotificationsResponse>>())!.Data!;
        inbox.UnreadCount.ShouldBe(0);
        inbox.Items.ShouldAllBe(notification => notification.ReadAt != null);
    }

    [Fact]
    public async Task Inbox_AnotherUser_SeesNothingOfOthers()
    {
        await AcceptedWalkAsync();
        HttpClient stranger = WalkScenario.Client(factory, "owner");

        NotificationsResponse inbox = await InboxAsync(stranger);

        (inbox.UnreadCount, inbox.Items.Count).ShouldBe((0, 0));
    }

    private async Task<(HttpClient Owner, HttpClient Walker, WalkResponse Walk)> AcceptedWalkAsync()
    {
        HttpClient owner = WalkScenario.Client(factory, "owner");
        HttpClient walker = await WalkScenario.VerifiedWalkerAsync(factory);
        WalkResponse walk = await WalkScenario.RequestAsync(owner);
        await WalkScenario.AcceptAsync(walker, walk.Id);
        return (owner, walker, walk);
    }

    private static async Task PostOkAsync(HttpClient client, Uri uri) =>
        (await client.PostAsync(uri, content: null)).StatusCode.ShouldBe(HttpStatusCode.OK);

    private static async Task<NotificationsResponse> InboxAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<WaggoApiResponse<NotificationsResponse>>(s_notifications))!.Data!;
}
