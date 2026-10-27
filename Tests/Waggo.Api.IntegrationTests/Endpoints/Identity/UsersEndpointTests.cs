using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Identity;

namespace Waggo.Api.IntegrationTests.Endpoints.Identity;

/// <summary>
/// Perfil de usuario: needs a real JWT (<see cref="JwtPipelineWaggoApiFactory"/>), since the handlers look up the
/// caller's own row in <c>identity.users</c> by the token's <c>sub</c> — the development-user bypass has no such
/// row for its opaque ids (e.g. <c>owner-...</c>).
/// </summary>
[Collection(JwtPipelineCollectionDefinition.Name)]
public class UsersEndpointTests(JwtPipelineWaggoApiFactory factory)
{
    private static readonly Uri s_me = new("/api/v1/users/me", UriKind.Relative);

    private async Task<HttpClient> SignedInClientAsync()
    {
        HttpClient client = factory.CreateClient();
        string email = $"ana-{Guid.NewGuid():N}@mail.com";
        await client.PostAsJsonAsync(new Uri("/api/v1/auth/register", UriKind.Relative), new { email, password = "Clave123!" });
        string code = factory.Emails.LastConfirmationCodeSentTo(email);
        await client.PostAsJsonAsync(new Uri("/api/v1/auth/confirm", UriKind.Relative), new { email, code });

        HttpResponseMessage login = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative), new { email, password = "Clave123!" });
        string accessToken = (await login.Content.ReadFromJsonAsync<WaggoApiResponse<SessionResponse>>())!.Data!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    [Fact]
    public async Task Get_NewAccount_ReturnsTheEmailWithNoFullNameOrPhoneYet()
    {
        using HttpClient client = await SignedInClientAsync();

        HttpResponseMessage response = await client.GetAsync(s_me);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        UserProfileResponse profile = (await response.Content.ReadFromJsonAsync<WaggoApiResponse<UserProfileResponse>>())!.Data!;
        profile.FullName.ShouldBeNull();
        profile.Phone.ShouldBeNull();
    }

    [Fact]
    public async Task Put_FullNameAndPhone_SavesThemAndGetReflectsIt()
    {
        using HttpClient client = await SignedInClientAsync();

        HttpResponseMessage put = await client.PutAsJsonAsync(s_me, new { fullName = "Ana Pérez", phone = "3001234567" });

        put.StatusCode.ShouldBe(HttpStatusCode.OK);
        UserProfileResponse updated = (await put.Content.ReadFromJsonAsync<WaggoApiResponse<UserProfileResponse>>())!.Data!;
        updated.FullName.ShouldBe("Ana Pérez");
        updated.Phone.ShouldBe("3001234567");

        HttpResponseMessage get = await client.GetAsync(s_me);
        UserProfileResponse fetched = (await get.Content.ReadFromJsonAsync<WaggoApiResponse<UserProfileResponse>>())!.Data!;
        fetched.FullName.ShouldBe("Ana Pérez");
        fetched.Phone.ShouldBe("3001234567");
    }

    [Fact]
    public async Task Put_FullNameTooLong_Returns400()
    {
        using HttpClient client = await SignedInClientAsync();

        HttpResponseMessage response = await client.PutAsJsonAsync(s_me, new { fullName = new string('a', 101), phone = (string?)null });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<WaggoApiResponse<object>>())!
            .Errors.Single().Code.ShouldBe("Identity.InvalidFullName");
    }

    [Fact]
    public async Task Get_NoToken_Returns401()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(s_me);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
