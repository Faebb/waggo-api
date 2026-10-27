using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Identity;
using Waggo.Application.Pets;

namespace Waggo.Api.IntegrationTests.Endpoints.Identity;

/// <summary>
/// With <see cref="JwtPipelineWaggoApiFactory"/> (no development user): a token issued by RF-005 must be what
/// <c>AddWaggoAuthentication</c> accepts for every other endpoint, end to end.
/// </summary>
[Collection(JwtPipelineCollectionDefinition.Name)]
public class JwtPipelineTests(JwtPipelineWaggoApiFactory factory)
{
    private async Task<string> IssueAccessTokenAsync(HttpClient client)
    {
        string email = $"ana-{Guid.NewGuid():N}@mail.com";
        await client.PostAsJsonAsync(new Uri("/api/v1/auth/register", UriKind.Relative), new { email, password = "Clave123!" });
        string code = factory.Emails.LastConfirmationCodeSentTo(email);
        await client.PostAsJsonAsync(new Uri("/api/v1/auth/confirm", UriKind.Relative), new { email, code });

        HttpResponseMessage login = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative), new { email, password = "Clave123!" });
        return (await login.Content.ReadFromJsonAsync<WaggoApiResponse<SessionResponse>>())!.Data!.AccessToken;
    }

    [Fact]
    public async Task RealAccessToken_AuthenticatesAProtectedEndpointAsItsOwner()
    {
        using HttpClient client = factory.CreateClient();
        string accessToken = await IssueAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/pets", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<WaggoApiResponse<List<PetResponse>>>())!.Data.ShouldBeEmpty();
    }

    [Fact]
    public async Task NoToken_IsRejectedWith401()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/pets", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TamperedToken_IsRejectedWith401()
    {
        using HttpClient client = factory.CreateClient();
        string accessToken = await IssueAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken + "x");

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/pets", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
