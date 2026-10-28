using System.Net;
using System.Net.Http.Json;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Identity;

namespace Waggo.Api.IntegrationTests.Endpoints.Identity;

/// <summary>Acceptance tests for RF-001 + RF-005 with Microsoft (ADR-017): <c>POST /auth/login/microsoft</c>.</summary>
[Collection(ApiCollectionDefinition.Name)]
public class MicrosoftLoginEndpointTests(WaggoApiFactory factory)
{
    private static readonly Uri s_microsoft = new("/api/v1/auth/login/microsoft", UriKind.Relative);
    private static readonly Uri s_google = new("/api/v1/auth/login/google", UriKind.Relative);
    private static readonly Uri s_register = new("/api/v1/auth/register", UriKind.Relative);
    private static readonly Uri s_login = new("/api/v1/auth/login", UriKind.Relative);

    private static string NewEmail() => $"ana-{Guid.NewGuid():N}@mail.com";

    private static string Token(string email, bool emailVerified = true) =>
        FakeExternalIdentityProvider.TokenFor($"tenant:{Guid.NewGuid():N}", email, emailVerified);

    private static async Task<string> SingleErrorCodeOf(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<WaggoApiResponse<object>>())!.Errors.Single().Code;

    [Fact]
    public async Task Login_NewEmail_CreatesTheAccountAndIssuesASession()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(s_microsoft, new { idToken = Token(NewEmail()) });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        SessionResponse session = (await response.Content.ReadFromJsonAsync<WaggoApiResponse<SessionResponse>>())!.Data!;
        session.AccessToken.ShouldNotBeNullOrWhiteSpace();
        session.RefreshToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_SameMicrosoftAccountTwice_EntersTheSameAccount()
    {
        using HttpClient client = factory.CreateClient();
        string token = Token(NewEmail());

        HttpResponseMessage first = await client.PostAsJsonAsync(s_microsoft, new { idToken = token });
        HttpResponseMessage second = await client.PostAsJsonAsync(s_microsoft, new { idToken = token });

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_EmailOfAPasswordAccount_LinksItAndThePasswordStillWorks()
    {
        using HttpClient client = factory.CreateClient();
        string email = NewEmail();
        await client.PostAsJsonAsync(s_register, new { email, password = "Clave123!" });
        await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/confirm", UriKind.Relative),
            new { email, code = factory.Emails.LastConfirmationCodeSentTo(email) });

        HttpResponseMessage microsoft = await client.PostAsJsonAsync(s_microsoft, new { idToken = Token(email) });
        HttpResponseMessage password = await client.PostAsJsonAsync(s_login, new { email, password = "Clave123!" });

        microsoft.StatusCode.ShouldBe(HttpStatusCode.OK);
        password.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_SameEmailThroughGoogleAndThenMicrosoft_BothEnterTheSameAccount()
    {
        using HttpClient client = factory.CreateClient();
        string email = NewEmail();
        string googleToken = FakeExternalIdentityProvider.TokenFor($"google-{Guid.NewGuid():N}", email);

        HttpResponseMessage google = await client.PostAsJsonAsync(s_google, new { idToken = googleToken });
        HttpResponseMessage microsoft = await client.PostAsJsonAsync(s_microsoft, new { idToken = Token(email) });

        google.StatusCode.ShouldBe(HttpStatusCode.OK);
        microsoft.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_EmailRegisteredButNeverConfirmed_TakesTheAccountOverAndKillsTheOldPassword()
    {
        using HttpClient client = factory.CreateClient();
        string email = NewEmail();
        await client.PostAsJsonAsync(s_register, new { email, password = "AttackerPass1" });

        HttpResponseMessage microsoft = await client.PostAsJsonAsync(s_microsoft, new { idToken = Token(email) });
        HttpResponseMessage oldPassword =
            await client.PostAsJsonAsync(s_login, new { email, password = "AttackerPass1" });

        microsoft.StatusCode.ShouldBe(HttpStatusCode.OK);
        oldPassword.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await SingleErrorCodeOf(oldPassword)).ShouldBe("Identity.InvalidCredentials");
    }

    [Fact]
    public async Task Login_EmailNotVerifiedByTheTenant_Returns422AndLinksNothing()
    {
        using HttpClient client = factory.CreateClient();
        string email = NewEmail();
        await client.PostAsJsonAsync(s_register, new { email, password = "Clave123!" });
        await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/confirm", UriKind.Relative),
            new { email, code = factory.Emails.LastConfirmationCodeSentTo(email) });

        HttpResponseMessage response =
            await client.PostAsJsonAsync(s_microsoft, new { idToken = Token(email, emailVerified: false) });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await SingleErrorCodeOf(response)).ShouldBe("Identity.ExternalEmailNotVerified");
    }

    [Fact]
    public async Task Login_ForgedToken_Returns400()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(s_microsoft, new { idToken = "forged-token" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await SingleErrorCodeOf(response)).ShouldBe("Identity.ExternalTokenInvalid");
    }

    [Fact]
    public async Task Login_MissingToken_Returns400WithRequestRequired()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(s_microsoft, new { });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await SingleErrorCodeOf(response)).ShouldBe("Request.Required");
    }
}
