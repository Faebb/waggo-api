using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Identity;

namespace Waggo.Api.IntegrationTests.Endpoints.Identity;

/// <summary>
/// DEV-001: in Testing the API seeds an admin, an owner and a verified walker at startup, so each role can sign in
/// with the real login (no development user) right away.
/// </summary>
[Collection(JwtPipelineCollectionDefinition.Name)]
public class SeededUsersTests(JwtPipelineWaggoApiFactory factory)
{
    private async Task<HttpClient> SignInAsync(string email, string password)
    {
        HttpClient client = factory.CreateClient();
        HttpResponseMessage login = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative), new { email, password });
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
        string accessToken = (await login.Content.ReadFromJsonAsync<WaggoApiResponse<SessionResponse>>())!
            .Data!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    [Fact]
    public async Task SeededWalker_SignsInWithTheWalkerRole_SeesAvailableWalks()
    {
        using HttpClient walker = await SignInAsync("paseador@waggo.local", "Paseador1234");

        HttpResponseMessage response = await walker.GetAsync(new Uri("/api/v1/walks/available", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SeededOwner_SignsInWithoutTheWalkerRole_IsForbiddenFromAvailableWalks()
    {
        using HttpClient owner = await SignInAsync("dueno@waggo.local", "Dueno1234");

        HttpResponseMessage response = await owner.GetAsync(new Uri("/api/v1/walks/available", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SeededAdmin_SignsInWithTheAdminRole_SeesTheWalkerReviewQueue()
    {
        using HttpClient admin = await SignInAsync("admin@waggo.local", "Admin1234");

        HttpResponseMessage response = await admin.GetAsync(new Uri("/api/v1/admin/walkers", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
