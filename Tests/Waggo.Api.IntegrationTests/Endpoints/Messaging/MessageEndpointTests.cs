using System.Net;
using System.Net.Http.Json;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Messaging;
using Waggo.Application.Walks;

namespace Waggo.Api.IntegrationTests.Endpoints.Messaging;

/// <summary>Acceptance tests for RF-013: the owner and the walker chat during the walk.</summary>
[Collection(ApiCollectionDefinition.Name)]
public class MessageEndpointTests(WaggoApiFactory factory)
{
    [Fact]
    public async Task OwnerAndWalker_ChatInOrder_AndCanAskOnlyForNewMessages()
    {
        using HttpClient owner = WalkScenario.Client(factory, "owner");
        using HttpClient walker = WalkScenario.Client(factory, "walker");
        WalkResponse walk = await WalkScenario.RequestAsync(owner);
        await WalkScenario.AcceptAsync(walker, walk.Id);

        HttpResponseMessage first = await owner.PostAsJsonAsync(Messages(walk.Id), new { text = "¿Ya llegaste?" });
        HttpResponseMessage second =
            await walker.PostAsJsonAsync(Messages(walk.Id), new { text = "Estoy en la portería" });
        List<WalkMessageResponse> all = await ReadAsync(owner, Messages(walk.Id));
        List<WalkMessageResponse> newer = await ReadAsync(walker, Messages(walk.Id, after: all[0].Id));

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        all.Select(message => (message.SentBy, message.Text))
            .ShouldBe([("Owner", "¿Ya llegaste?"), ("Walker", "Estoy en la portería")]);
        newer.Select(message => message.Text).ShouldBe(["Estoy en la portería"]);
    }

    [Fact]
    public async Task Stranger_CannotReadOrWrite()
    {
        using HttpClient owner = WalkScenario.Client(factory, "owner");
        using HttpClient walker = WalkScenario.Client(factory, "walker");
        using HttpClient stranger = WalkScenario.Client(factory, "owner,walker");
        WalkResponse walk = await WalkScenario.RequestAsync(owner);
        await WalkScenario.AcceptAsync(walker, walk.Id);

        HttpResponseMessage write = await stranger.PostAsJsonAsync(Messages(walk.Id), new { text = "Hola" });
        HttpResponseMessage read = await stranger.GetAsync(Messages(walk.Id));

        write.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        read.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FinishedWalk_ChatIsReadOnly()
    {
        using HttpClient owner = WalkScenario.Client(factory, "owner");
        using HttpClient walker = WalkScenario.Client(factory, "walker");
        WalkResponse walk = await WalkScenario.RequestAsync(owner);
        await WalkScenario.AcceptAsync(walker, walk.Id, start: true);
        await owner.PostAsJsonAsync(Messages(walk.Id), new { text = "Gracias" });
        await walker.PostAsync(WalkScenario.WalkUri(walk.Id, "finish"), content: null);

        HttpResponseMessage late = await owner.PostAsJsonAsync(Messages(walk.Id), new { text = "¿Hola?" });
        List<WalkMessageResponse> history = await ReadAsync(owner, Messages(walk.Id));

        late.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        await WalkScenario.ShouldHaveSingleErrorAsync(late, "Messages.ChatClosed");
        history.Select(message => message.Text).ShouldBe(["Gracias"]);
    }

    [Fact]
    public async Task EmptyText_Returns400()
    {
        using HttpClient owner = WalkScenario.Client(factory, "owner");
        using HttpClient walker = WalkScenario.Client(factory, "walker");
        WalkResponse walk = await WalkScenario.RequestAsync(owner);
        await WalkScenario.AcceptAsync(walker, walk.Id);

        HttpResponseMessage response = await owner.PostAsJsonAsync(Messages(walk.Id), new { text = "   " });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await WalkScenario.ShouldHaveSingleErrorAsync(response, "Messages.InvalidText");
    }

    private static Uri Messages(Guid walkId, Guid? after = null) =>
        new($"/api/v1/walks/{walkId}/messages{(after is null ? string.Empty : $"?after={after}")}", UriKind.Relative);

    private static async Task<List<WalkMessageResponse>> ReadAsync(HttpClient client, Uri uri) =>
        (await client.GetFromJsonAsync<WaggoApiResponse<List<WalkMessageResponse>>>(uri))!.Data!;
}
