using System.Net;
using System.Net.Http.Json;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Application.Pets;
using Waggo.Application.Walkers;
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

    /// <summary>
    /// A walker who registered and was approved by an admin (RF-002, RF-003): only they see and accept requests.
    /// </summary>
    public static async Task<HttpClient> VerifiedWalkerAsync(
        WaggoApiFactory factory,
        string? userId = null,
        string roles = "walker")
    {
        HttpClient walker = Client(factory, roles);
        if (userId is not null)
        {
            walker.DefaultRequestHeaders.Remove("X-Dev-User-Id");
            walker.DefaultRequestHeaders.Add("X-Dev-User-Id", userId);
        }

        HttpResponseMessage registered =
            await walker.PostAsJsonAsync(new Uri("/api/v1/walkers/me", UriKind.Relative), WalkerProfileRequest());
        registered.StatusCode.ShouldBe(HttpStatusCode.OK);
        Guid profileId = (await registered.Content.ReadFromJsonAsync<WaggoApiResponse<WalkerProfileResponse>>())!
            .Data!.Id;

        using HttpClient admin = Client(factory, "admin");
        HttpResponseMessage approved = await admin.PostAsync(
            new Uri($"/api/v1/admin/walkers/{profileId}/approve", UriKind.Relative),
            content: null);
        approved.StatusCode.ShouldBe(HttpStatusCode.OK);
        return walker;
    }

    public static object WalkerProfileRequest(string phone = "3001234567") => new
    {
        fullName = "Andrés Gómez",
        documentType = "CC",
        documentNumber = "1020304050",
        phone,
        experience = "3 años con perros grandes",
    };

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
