using System.Net;
using System.Net.Http.Json;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Identity;

namespace Waggo.Api.IntegrationTests.Endpoints.Identity;

/// <summary>Acceptance tests for RF-005, RNF-002: signing out revokes the refresh token on the server.</summary>
[Collection(ApiCollectionDefinition.Name)]
public class LogoutEndpointTests(WaggoApiFactory factory)
{
    private static readonly Uri s_register = new("/api/v1/auth/register", UriKind.Relative);
    private static readonly Uri s_confirm = new("/api/v1/auth/confirm", UriKind.Relative);
    private static readonly Uri s_login = new("/api/v1/auth/login", UriKind.Relative);
    private static readonly Uri s_refresh = new("/api/v1/auth/refresh", UriKind.Relative);
    private static readonly Uri s_logout = new("/api/v1/auth/logout", UriKind.Relative);

    private async Task<string> SignedInRefreshTokenAsync(HttpClient client)
    {
        string email = $"ana-{Guid.NewGuid():N}@mail.com";
        await client.PostAsJsonAsync(s_register, new { email, password = "Clave123!" });
        string code = factory.Emails.LastConfirmationCodeSentTo(email);
        await client.PostAsJsonAsync(s_confirm, new { email, code });
        HttpResponseMessage login = await client.PostAsJsonAsync(s_login, new { email, password = "Clave123!" });
        return (await login.Content.ReadFromJsonAsync<WaggoApiResponse<SessionResponse>>())!.Data!.RefreshToken;
    }

    [Fact]
    public async Task Logout_ActiveRefreshToken_Returns200AndTheTokenStopsWorking()
    {
        using HttpClient client = factory.CreateClient();
        string refreshToken = await SignedInRefreshTokenAsync(client);

        HttpResponseMessage logout = await client.PostAsJsonAsync(s_logout, new { refreshToken });

        logout.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await logout.Content.ReadFromJsonAsync<WaggoApiResponse<bool>>())!.Data.ShouldBeTrue();
        HttpResponseMessage refresh = await client.PostAsJsonAsync(s_refresh, new { refreshToken });
        refresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_UnknownRefreshToken_Returns200()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(s_logout, new { refreshToken = "not-a-token" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<WaggoApiResponse<bool>>())!.Data.ShouldBeTrue();
    }

    [Fact]
    public async Task Logout_SameTokenTwice_Returns200BothTimes()
    {
        using HttpClient client = factory.CreateClient();
        string refreshToken = await SignedInRefreshTokenAsync(client);
        await client.PostAsJsonAsync(s_logout, new { refreshToken });

        HttpResponseMessage again = await client.PostAsJsonAsync(s_logout, new { refreshToken });

        again.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_OnlyEndsThatSession_TheOtherDeviceKeepsWorking()
    {
        using HttpClient client = factory.CreateClient();
        string email = $"ana-{Guid.NewGuid():N}@mail.com";
        await client.PostAsJsonAsync(s_register, new { email, password = "Clave123!" });
        await client.PostAsJsonAsync(s_confirm, new { email, code = factory.Emails.LastConfirmationCodeSentTo(email) });
        string phone = await LoginTokenAsync(client, email);
        string tablet = await LoginTokenAsync(client, email);

        await client.PostAsJsonAsync(s_logout, new { refreshToken = phone });

        HttpResponseMessage refresh = await client.PostAsJsonAsync(s_refresh, new { refreshToken = tablet });
        refresh.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_WithoutRefreshToken_Returns400()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(s_logout, new { });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<WaggoApiResponse<object>>())!
            .Errors.Single().Code.ShouldBe("Request.Required");
    }

    private static async Task<string> LoginTokenAsync(HttpClient client, string email)
    {
        HttpResponseMessage login = await client.PostAsJsonAsync(s_login, new { email, password = "Clave123!" });
        return (await login.Content.ReadFromJsonAsync<WaggoApiResponse<SessionResponse>>())!.Data!.RefreshToken;
    }
}
