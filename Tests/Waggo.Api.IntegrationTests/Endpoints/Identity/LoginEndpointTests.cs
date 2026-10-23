using System.Net;
using System.Net.Http.Json;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Identity;

namespace Waggo.Api.IntegrationTests.Endpoints.Identity;

/// <summary>Acceptance tests for RF-005: password + email code, then a rotating session.</summary>
[Collection(ApiCollectionDefinition.Name)]
public class LoginEndpointTests(WaggoApiFactory factory)
{
    private static readonly Uri s_register = new("/api/v1/auth/register", UriKind.Relative);
    private static readonly Uri s_login = new("/api/v1/auth/login", UriKind.Relative);
    private static readonly Uri s_verify = new("/api/v1/auth/login/verify", UriKind.Relative);
    private static readonly Uri s_refresh = new("/api/v1/auth/refresh", UriKind.Relative);
    private static readonly Uri s_resendCode = new("/api/v1/auth/resend-code", UriKind.Relative);
    private static readonly Uri s_logout = new("/api/v1/auth/logout", UriKind.Relative);

    private async Task<string> RegisterAndConfirmAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync(s_register, new { email, password = "Clave123!" });
        string token = ExtractCode(factory.Emails.LastBodySentTo(email));
        await client.GetAsync(new Uri($"/api/v1/auth/confirm?token={token}", UriKind.Relative));
        return email;
    }

    private static async Task<Guid> LoginAsync(HttpClient client, string email, string password = "Clave123!")
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(s_login, new { email, password });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<WaggoApiResponse<LoginResponse>>())!.Data!.ChallengeId;
    }

    private static string ExtractCode(string emailBody) => emailBody.Split(": ")[^1];

    [Fact]
    public async Task FullFlow_RegisterConfirmLoginVerify_IssuesASession()
    {
        using HttpClient client = factory.CreateClient();
        string email = $"ana-{Guid.NewGuid():N}@mail.com";
        await RegisterAndConfirmAsync(client, email);
        Guid challengeId = await LoginAsync(client, email);
        string code = ExtractCode(factory.Emails.LastBodySentTo(email));

        HttpResponseMessage response = await client.PostAsJsonAsync(s_verify, new { challengeId, code });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        SessionResponse session = (await response.Content.ReadFromJsonAsync<WaggoApiResponse<SessionResponse>>())!.Data!;
        session.AccessToken.ShouldNotBeNullOrWhiteSpace();
        session.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        session.ExpiresInSeconds.ShouldBe(15 * 60);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns400WithInvalidCredentials()
    {
        using HttpClient client = factory.CreateClient();
        string email = $"ana-{Guid.NewGuid():N}@mail.com";
        await RegisterAndConfirmAsync(client, email);

        HttpResponseMessage response =
            await client.PostAsJsonAsync(s_login, new { email, password = "WrongPassword1" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<WaggoApiResponse<object>>())!
            .Errors.Single().Code.ShouldBe("Identity.InvalidCredentials");
    }

    [Fact]
    public async Task Login_UnconfirmedAccount_Returns422()
    {
        using HttpClient client = factory.CreateClient();
        string email = $"ana-{Guid.NewGuid():N}@mail.com";
        await client.PostAsJsonAsync(s_register, new { email, password = "Clave123!" });

        HttpResponseMessage response = await client.PostAsJsonAsync(s_login, new { email, password = "Clave123!" });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadFromJsonAsync<WaggoApiResponse<object>>())!
            .Errors.Single().Code.ShouldBe("Identity.AccountNotConfirmed");
    }

    [Fact]
    public async Task VerifyLoginCode_WrongCode_Returns400WithInvalidCode()
    {
        using HttpClient client = factory.CreateClient();
        string email = $"ana-{Guid.NewGuid():N}@mail.com";
        await RegisterAndConfirmAsync(client, email);
        Guid challengeId = await LoginAsync(client, email);

        HttpResponseMessage response =
            await client.PostAsJsonAsync(s_verify, new { challengeId, code = "000000" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<WaggoApiResponse<object>>())!
            .Errors.Single().Code.ShouldBe("Identity.InvalidCode");
    }

    [Fact]
    public async Task Refresh_ValidToken_RotatesItAndIssuesANewSession()
    {
        using HttpClient client = factory.CreateClient();
        string email = $"ana-{Guid.NewGuid():N}@mail.com";
        await RegisterAndConfirmAsync(client, email);
        Guid challengeId = await LoginAsync(client, email);
        string code = ExtractCode(factory.Emails.LastBodySentTo(email));
        SessionResponse first = (await (await client.PostAsJsonAsync(s_verify, new { challengeId, code }))
            .Content.ReadFromJsonAsync<WaggoApiResponse<SessionResponse>>())!.Data!;

        HttpResponseMessage response = await client.PostAsJsonAsync(s_refresh, new { refreshToken = first.RefreshToken });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        SessionResponse second = (await response.Content.ReadFromJsonAsync<WaggoApiResponse<SessionResponse>>())!.Data!;
        second.RefreshToken.ShouldNotBe(first.RefreshToken);
        second.AccessToken.ShouldNotBe(first.AccessToken);
    }

    [Fact]
    public async Task ResendCode_PendingChallenge_SameChallengeAndOnlyTheNewCodeWorks()
    {
        using HttpClient client = factory.CreateClient();
        string email = $"ana-{Guid.NewGuid():N}@mail.com";
        await RegisterAndConfirmAsync(client, email);
        Guid challengeId = await LoginAsync(client, email);
        string oldCode = ExtractCode(factory.Emails.LastBodySentTo(email));

        HttpResponseMessage resent = await client.PostAsJsonAsync(s_resendCode, new { challengeId });

        resent.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await resent.Content.ReadFromJsonAsync<WaggoApiResponse<LoginResponse>>())!.Data!.ChallengeId
            .ShouldBe(challengeId);
        string newCode = ExtractCode(factory.Emails.LastBodySentTo(email));
        if (newCode != oldCode)
        {
            HttpResponseMessage withOldCode = await client.PostAsJsonAsync(s_verify, new { challengeId, code = oldCode });
            withOldCode.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        HttpResponseMessage withNewCode = await client.PostAsJsonAsync(s_verify, new { challengeId, code = newCode });
        withNewCode.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResendCode_UnknownChallenge_Returns404WithChallengeNotFound()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(s_resendCode, new { challengeId = Guid.NewGuid() });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadFromJsonAsync<WaggoApiResponse<object>>())!
            .Errors.Single().Code.ShouldBe("Identity.ChallengeNotFound");
    }

    [Fact]
    public async Task Logout_ValidToken_SucceedsAndTheTokenNoLongerRefreshes()
    {
        using HttpClient client = factory.CreateClient();
        string email = $"ana-{Guid.NewGuid():N}@mail.com";
        await RegisterAndConfirmAsync(client, email);
        Guid challengeId = await LoginAsync(client, email);
        string code = ExtractCode(factory.Emails.LastBodySentTo(email));
        SessionResponse session = (await (await client.PostAsJsonAsync(s_verify, new { challengeId, code }))
            .Content.ReadFromJsonAsync<WaggoApiResponse<SessionResponse>>())!.Data!;

        HttpResponseMessage logout = await client.PostAsJsonAsync(s_logout, new { refreshToken = session.RefreshToken });

        logout.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await logout.Content.ReadFromJsonAsync<WaggoApiResponse<bool>>())!.Success.ShouldBeTrue();
        HttpResponseMessage refresh = await client.PostAsJsonAsync(s_refresh, new { refreshToken = session.RefreshToken });
        refresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_UnknownToken_StillSucceeds()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(s_logout, new { refreshToken = "not-a-real-token" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<WaggoApiResponse<bool>>())!.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task Refresh_AlreadyRotatedToken_Returns401AndTheNewOneStopsWorkingToo()
    {
        using HttpClient client = factory.CreateClient();
        string email = $"ana-{Guid.NewGuid():N}@mail.com";
        await RegisterAndConfirmAsync(client, email);
        Guid challengeId = await LoginAsync(client, email);
        string code = ExtractCode(factory.Emails.LastBodySentTo(email));
        SessionResponse first = (await (await client.PostAsJsonAsync(s_verify, new { challengeId, code }))
            .Content.ReadFromJsonAsync<WaggoApiResponse<SessionResponse>>())!.Data!;
        SessionResponse second = (await (await client.PostAsJsonAsync(s_refresh, new { refreshToken = first.RefreshToken }))
            .Content.ReadFromJsonAsync<WaggoApiResponse<SessionResponse>>())!.Data!;

        // Reusing the already-rotated first token: a stolen-token signal that revokes the whole family.
        HttpResponseMessage reuse = await client.PostAsJsonAsync(s_refresh, new { refreshToken = first.RefreshToken });
        reuse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        HttpResponseMessage secondRefresh = await client.PostAsJsonAsync(s_refresh, new { refreshToken = second.RefreshToken });
        secondRefresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
