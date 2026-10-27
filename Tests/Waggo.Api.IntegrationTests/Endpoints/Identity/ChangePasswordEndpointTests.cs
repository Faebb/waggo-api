using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Identity;

namespace Waggo.Api.IntegrationTests.Endpoints.Identity;

/// <summary>
/// Acceptance tests for RF-005: a signed-in account changes its password. Needs a real JWT
/// (<see cref="JwtPipelineWaggoApiFactory"/>), since the handler looks up the caller's own row by the token's <c>sub</c>.
/// </summary>
[Collection(JwtPipelineCollectionDefinition.Name)]
public class ChangePasswordEndpointTests(JwtPipelineWaggoApiFactory factory)
{
    private const string Current = "Clave123!";
    private const string NewPassword = "Nueva1234";

    private static readonly Uri s_register = new("/api/v1/auth/register", UriKind.Relative);
    private static readonly Uri s_confirm = new("/api/v1/auth/confirm", UriKind.Relative);
    private static readonly Uri s_login = new("/api/v1/auth/login", UriKind.Relative);
    private static readonly Uri s_refresh = new("/api/v1/auth/refresh", UriKind.Relative);
    private static readonly Uri s_password = new("/api/v1/users/me/password", UriKind.Relative);

    private static async Task<string> FirstErrorCodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<WaggoApiResponse<object>>())!.Errors.Single().Code;

    private async Task<string> ConfirmedAccountAsync(HttpClient client)
    {
        string email = $"ana-{Guid.NewGuid():N}@mail.com";
        await client.PostAsJsonAsync(s_register, new { email, password = Current });
        string code = factory.Emails.LastConfirmationCodeSentTo(email);
        await client.PostAsJsonAsync(s_confirm, new { email, code });
        return email;
    }

    private static async Task<SessionResponse> LoginAsync(HttpClient client, string email, string password)
    {
        HttpResponseMessage login = await client.PostAsJsonAsync(s_login, new { email, password });
        return (await login.Content.ReadFromJsonAsync<WaggoApiResponse<SessionResponse>>())!.Data!;
    }

    private static HttpClient AsSession(HttpClient client, SessionResponse session)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }

    [Fact]
    public async Task Put_RightCurrentPassword_ChangesItAndTheOldOneStopsWorking()
    {
        using HttpClient client = factory.CreateClient();
        string email = await ConfirmedAccountAsync(client);
        SessionResponse session = await LoginAsync(client, email, Current);
        AsSession(client, session);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            s_password, new { currentPassword = Current, newPassword = NewPassword, refreshToken = session.RefreshToken });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<WaggoApiResponse<bool>>())!.Data.ShouldBeTrue();
        using HttpClient anonymous = factory.CreateClient();
        HttpResponseMessage oldLogin = await anonymous.PostAsJsonAsync(s_login, new { email, password = Current });
        (await FirstErrorCodeAsync(oldLogin)).ShouldBe("Identity.InvalidCredentials");
        HttpResponseMessage newLogin = await anonymous.PostAsJsonAsync(s_login, new { email, password = NewPassword });
        newLogin.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Put_RightCurrentPassword_KeepsThisSessionAndClosesTheOthers()
    {
        using HttpClient client = factory.CreateClient();
        string email = await ConfirmedAccountAsync(client);
        SessionResponse thisDevice = await LoginAsync(client, email, Current);
        SessionResponse otherDevice = await LoginAsync(client, email, Current);
        AsSession(client, thisDevice);

        await client.PutAsJsonAsync(
            s_password, new { currentPassword = Current, newPassword = NewPassword, refreshToken = thisDevice.RefreshToken });

        using HttpClient anonymous = factory.CreateClient();
        HttpResponseMessage mine = await anonymous.PostAsJsonAsync(s_refresh, new { refreshToken = thisDevice.RefreshToken });
        mine.StatusCode.ShouldBe(HttpStatusCode.OK);
        HttpResponseMessage other = await anonymous.PostAsJsonAsync(s_refresh, new { refreshToken = otherDevice.RefreshToken });
        other.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_RightCurrentPassword_EmailsASecurityNotice()
    {
        using HttpClient client = factory.CreateClient();
        string email = await ConfirmedAccountAsync(client);
        SessionResponse session = await LoginAsync(client, email, Current);
        AsSession(client, session);
        int before = factory.Emails.SentCountTo(email);

        await client.PutAsJsonAsync(
            s_password, new { currentPassword = Current, newPassword = NewPassword, refreshToken = session.RefreshToken });

        factory.Emails.SentCountTo(email).ShouldBe(before + 1);
        factory.Emails.LastBodySentTo(email).ShouldContain("cambió");
    }

    [Fact]
    public async Task Put_WrongCurrentPassword_Returns422AndKeepsThePassword()
    {
        using HttpClient client = factory.CreateClient();
        string email = await ConfirmedAccountAsync(client);
        SessionResponse session = await LoginAsync(client, email, Current);
        AsSession(client, session);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            s_password, new { currentPassword = "Equivocada1", newPassword = NewPassword, refreshToken = session.RefreshToken });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await FirstErrorCodeAsync(response)).ShouldBe("Identity.CurrentPasswordIncorrect");
        using HttpClient anonymous = factory.CreateClient();
        (await anonymous.PostAsJsonAsync(s_login, new { email, password = Current })).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Put_NewPasswordEqualToTheCurrentOne_Returns422()
    {
        using HttpClient client = factory.CreateClient();
        string email = await ConfirmedAccountAsync(client);
        SessionResponse session = await LoginAsync(client, email, Current);
        AsSession(client, session);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            s_password, new { currentPassword = Current, newPassword = Current, refreshToken = session.RefreshToken });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await FirstErrorCodeAsync(response)).ShouldBe("Identity.PasswordUnchanged");
    }

    [Fact]
    public async Task Put_WeakNewPassword_Returns400()
    {
        using HttpClient client = factory.CreateClient();
        string email = await ConfirmedAccountAsync(client);
        SessionResponse session = await LoginAsync(client, email, Current);
        AsSession(client, session);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            s_password, new { currentPassword = Current, newPassword = "corta", refreshToken = session.RefreshToken });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await FirstErrorCodeAsync(response)).ShouldBe("Identity.WeakPassword");
    }

    [Fact]
    public async Task Put_FiveWrongCurrentPasswords_LockTheAccountEvenForTheRightOne()
    {
        using HttpClient client = factory.CreateClient();
        string email = await ConfirmedAccountAsync(client);
        SessionResponse session = await LoginAsync(client, email, Current);
        AsSession(client, session);
        for (int attempt = 0; attempt < 5; attempt++)
        {
            await client.PutAsJsonAsync(
                s_password, new { currentPassword = "Equivocada1", newPassword = NewPassword, refreshToken = session.RefreshToken });
        }

        HttpResponseMessage blocked = await client.PutAsJsonAsync(
            s_password, new { currentPassword = Current, newPassword = NewPassword, refreshToken = session.RefreshToken });

        blocked.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await FirstErrorCodeAsync(blocked)).ShouldBe("Identity.TooManyAttempts");
    }

    [Fact]
    public async Task Put_MissingFields_Returns400()
    {
        using HttpClient client = factory.CreateClient();
        string email = await ConfirmedAccountAsync(client);
        AsSession(client, await LoginAsync(client, email, Current));

        HttpResponseMessage response = await client.PutAsJsonAsync(s_password, new { });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        WaggoApiResponse<object> body = (await response.Content.ReadFromJsonAsync<WaggoApiResponse<object>>())!;
        body.Errors.Count.ShouldBe(2);
        body.Errors.ShouldAllBe(error => error.Code == "Request.Required");
    }

    [Fact]
    public async Task Put_WithoutASession_Returns401()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PutAsJsonAsync(
            s_password, new { currentPassword = Current, newPassword = NewPassword });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
