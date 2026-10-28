using System.Net;
using System.Text.Json;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Walks;

namespace Waggo.Api.IntegrationTests.Endpoints.Walks;

/// <summary>
/// RF-007 examples 15–17 and RF-008 example 13: the walk tells its owner who the assigned walker is, and nobody
/// else sees it. The JSON is read raw to check the wire contract (<c>walker.id</c>, <c>walker.fullName</c>).
/// </summary>
[Collection(ApiCollectionDefinition.Name)]
public class AssignedWalkerEndpointTests(WaggoApiFactory factory)
{
    private static readonly Uri s_walks = new("/api/v1/walks", UriKind.Relative);

    [Fact]
    public async Task GetById_WalkWithoutWalker_ReturnsWalkerNull()
    {
        using HttpClient owner = WalkScenario.Client(factory, "owner");
        WalkResponse walk = await WalkScenario.RequestAsync(owner);

        JsonElement data = await DataAsync(await owner.GetAsync(WalkUri(walk.Id)));

        data.GetProperty("walker").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetById_AcceptedWalk_TheOwnerSeesTheAssignedWalker()
    {
        string walkerId = $"walker-{Guid.NewGuid():N}";
        using HttpClient owner = WalkScenario.Client(factory, "owner");
        using HttpClient walker = await WalkScenario.VerifiedWalkerAsync(factory, walkerId);
        WalkResponse walk = await WalkScenario.RequestAsync(owner);
        await WalkScenario.AcceptAsync(walker, walk.Id);

        JsonElement detail = await DataAsync(await owner.GetAsync(WalkUri(walk.Id)));
        JsonElement listed = (await DataAsync(await owner.GetAsync(s_walks)))
            .EnumerateArray()
            .Single(item => item.GetProperty("id").GetGuid() == walk.Id);

        ShouldBeTheWalker(detail.GetProperty("walker"), walkerId);
        ShouldBeTheWalker(listed.GetProperty("walker"), walkerId);
    }

    [Fact]
    public async Task StartWalk_InProgress_TheWalkerAndTheOwnerSeeTheAssignedWalker()
    {
        string walkerId = $"walker-{Guid.NewGuid():N}";
        using HttpClient owner = WalkScenario.Client(factory, "owner");
        using HttpClient walker = await WalkScenario.VerifiedWalkerAsync(factory, walkerId);
        WalkResponse walk = await WalkScenario.RequestAsync(owner);
        await WalkScenario.AcceptAsync(walker, walk.Id);

        JsonElement started =
            await DataAsync(await walker.PostAsync(WalkScenario.WalkUri(walk.Id, "start"), content: null));
        JsonElement seenByOwner = await DataAsync(await owner.GetAsync(WalkUri(walk.Id)));

        started.GetProperty("status").GetString().ShouldBe("InProgress");
        ShouldBeTheWalker(started.GetProperty("walker"), walkerId);
        ShouldBeTheWalker(seenByOwner.GetProperty("walker"), walkerId);
    }

    [Fact]
    public async Task GetById_AcceptedWalkSeenByAnotherOwnerOrWalker_Returns404WithoutTheWalker()
    {
        using HttpClient owner = WalkScenario.Client(factory, "owner");
        using HttpClient walker = await WalkScenario.VerifiedWalkerAsync(factory);
        using HttpClient otherOwner = WalkScenario.Client(factory, "owner");
        using HttpClient otherWalker = await WalkScenario.VerifiedWalkerAsync(factory);
        WalkResponse walk = await WalkScenario.RequestAsync(owner);
        await WalkScenario.AcceptAsync(walker, walk.Id);

        foreach (HttpClient stranger in new[] { otherOwner, otherWalker })
        {
            HttpResponseMessage response = await stranger.GetAsync(WalkUri(walk.Id));

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            string body = await response.Content.ReadAsStringAsync();
            body.ShouldNotContain("Andrés Gómez");
            await WalkScenario.ShouldHaveSingleErrorAsync(response, "Walks.NotFound");
        }
    }

    private static void ShouldBeTheWalker(JsonElement walker, string walkerId)
    {
        walker.ValueKind.ShouldBe(JsonValueKind.Object);
        walker.GetProperty("id").GetString().ShouldBe(walkerId);
        walker.GetProperty("fullName").GetString().ShouldBe("Andrés Gómez");
    }

    private static async Task<JsonElement> DataAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("data").Clone();
    }

    private static Uri WalkUri(Guid walkId) => new($"/api/v1/walks/{walkId}", UriKind.Relative);
}
