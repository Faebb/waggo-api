using System.Net;
using System.Net.Http.Json;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Application.Pets;
using Waggo.Application.Walks;

namespace Waggo.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Builds the usual starting point of the walk tests: an owner with a dog who requested a walk, and (optionally) a
/// walker who accepted and started it. Each call uses brand-new users, so tests never see each other's data.
/// </summary>
internal static class WalkScenario
{
    public static HttpClient Client(WaggoApiFactory factory, string roles)
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User-Id", $"{roles.Split(',')[0]}-{Guid.NewGuid():N}");
        client.DefaultRequestHeaders.Add("X-Dev-Roles", roles);
        return client;
    }

    public static Uri WalkUri(Guid walkId, string action) =>
        new($"/api/v1/walks/{walkId}/{action}", UriKind.Relative);

    public static async Task<WalkResponse> RequestAsync(HttpClient owner)
    {
        HttpResponseMessage pet = await owner.PostAsJsonAsync(
            new Uri("/api/v1/pets", UriKind.Relative),
            new { name = "Luna", size = "Medium" });
        Guid petId = (await pet.Content.ReadFromJsonAsync<WaggoApiResponse<PetResponse>>())!.Data!.Id;

        HttpResponseMessage requested = await owner.PostAsJsonAsync(new Uri("/api/v1/walks", UriKind.Relative), new
        {
            petIds = new[] { petId },
            walkType = "Individual",
            durationMinutes = 60,
            pickupAddress = "Cra 7 # 45-10, Bogotá",
            latitude = 4.6361,
            longitude = -74.0645,
        });
        requested.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await requested.Content.ReadFromJsonAsync<WaggoApiResponse<WalkResponse>>())!.Data!;
    }

    /// <summary>The walker accepts the owner's walk and, when <paramref name="start"/>, starts it.</summary>
    public static async Task AcceptAsync(HttpClient walker, Guid walkId, bool start = false)
    {
        (await walker.PostAsync(WalkUri(walkId, "accept"), content: null)).StatusCode.ShouldBe(HttpStatusCode.OK);
        if (start)
        {
            (await walker.PostAsync(WalkUri(walkId, "start"), content: null)).StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    public static async Task ShouldHaveSingleErrorAsync(HttpResponseMessage response, string code)
    {
        WaggoApiResponse<object>? body = await response.Content.ReadFromJsonAsync<WaggoApiResponse<object>>();
        body!.Success.ShouldBeFalse();
        body.Errors.Single().Code.ShouldBe(code);
    }
}
