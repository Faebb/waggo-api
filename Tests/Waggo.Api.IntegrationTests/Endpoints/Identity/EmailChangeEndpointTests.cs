using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Identity;
using Waggo.Domain.Entities.Identity;
using Waggo.Infrastructure.Persistence.Context;

namespace Waggo.Api.IntegrationTests.Endpoints.Identity;

/// <summary>
/// Acceptance tests for RF-005: a signed-in account changes its email, proving it owns the new inbox with a 6-digit
/// code. Needs a real JWT (<see cref="JwtPipelineWaggoApiFactory"/>).
/// </summary>
[Collection(JwtPipelineCollectionDefinition.Name)]
public class EmailChangeEndpointTests(JwtPipelineWaggoApiFactory factory)
{
    private const string Password = "Clave123!";

    private static readonly Uri s_register = new("/api/v1/auth/register", UriKind.Relative);
    private static readonly Uri s_confirm = new("/api/v1/auth/confirm", UriKind.Relative);
    private static readonly Uri s_login = new("/api/v1/auth/login", UriKind.Relative);
    private static readonly Uri s_refresh = new("/api/v1/auth/refresh", UriKind.Relative);
    private static readonly Uri s_me = new("/api/v1/users/me", UriKind.Relative);
    private static readonly Uri s_request = new("/api/v1/users/me/email", UriKind.Relative);
    private static readonly Uri s_confirmChange = new("/api/v1/users/me/email/confirm", UriKind.Relative);

    private static string NewEmail() => $"ana-{Guid.NewGuid():N}@mail.com";

    private static async Task<string> FirstErrorCodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<WaggoApiResponse<object>>())!.Errors.Single().Code;

    private async Task<string> ConfirmedAccountAsync(HttpClient client)
    {
        string email = NewEmail();
        await client.PostAsJsonAsync(s_register, new { email, password = Password });
        string code = factory.Emails.LastConfirmationCodeSentTo(email);
        await client.PostAsJsonAsync(s_confirm, new { email, code });
        return email;
    }

    private static async Task<SessionResponse> LoginAsync(HttpClient client, string email, string password = Password)
    {
        HttpResponseMessage login = await client.PostAsJsonAsync(s_login, new { email, password });
        return (await login.Content.ReadFromJsonAsync<WaggoApiResponse<SessionResponse>>())!.Data!;
    }

    private static HttpClient AsSession(HttpClient client, SessionResponse session)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }

    private async Task<(HttpClient Client, string Email, SessionResponse Session)> SignedInAsync()
    {
        HttpClient anonymous = factory.CreateClient();
        string email = await ConfirmedAccountAsync(anonymous);
        SessionResponse session = await LoginAsync(anonymous, email);
        return (AsSession(anonymous, session), email, session);
    }

    private async Task<string> RequestChangeAsync(HttpClient client, string newEmail)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(s_request, new { newEmail, password = Password });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return factory.Emails.LastConfirmationCodeSentTo(newEmail);
    }

    [Fact]
    public async Task Request_FreeEmailAndRightPassword_EmailsACodeToTheNewAddressOnly()
    {
        (HttpClient client, string oldEmail, _) = await SignedInAsync();
        using HttpClient _ = client;
        string newEmail = NewEmail();
        int oldBefore = factory.Emails.SentCountTo(oldEmail);

        HttpResponseMessage response = await client.PostAsJsonAsync(s_request, new { newEmail, password = Password });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<WaggoApiResponse<bool>>())!.Data.ShouldBeTrue();
        factory.Emails.SentCountTo(newEmail).ShouldBe(1);
        factory.Emails.SentCountTo(oldEmail).ShouldBe(oldBefore);
        UserProfileResponse profile =
            (await (await client.GetAsync(s_me)).Content.ReadFromJsonAsync<WaggoApiResponse<UserProfileResponse>>())!.Data!;
        profile.Email.ShouldBe(oldEmail);
    }

    [Fact]
    public async Task Confirm_RightCode_ChangesTheEmailAndTheLoginUsesTheNewOne()
    {
        (HttpClient client, string oldEmail, SessionResponse session) = await SignedInAsync();
        using HttpClient _ = client;
        string newEmail = NewEmail();
        string code = await RequestChangeAsync(client, newEmail);
        int oldBefore = factory.Emails.SentCountTo(oldEmail);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            s_confirmChange, new { code, refreshToken = session.RefreshToken });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        UserProfileResponse profile =
            (await (await client.GetAsync(s_me)).Content.ReadFromJsonAsync<WaggoApiResponse<UserProfileResponse>>())!.Data!;
        profile.Email.ShouldBe(newEmail);
        using HttpClient anonymous = factory.CreateClient();
        (await anonymous.PostAsJsonAsync(s_login, new { email = newEmail, password = Password }))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
        (await FirstErrorCodeAsync(await anonymous.PostAsJsonAsync(s_login, new { email = oldEmail, password = Password })))
            .ShouldBe("Identity.InvalidCredentials");
        factory.Emails.SentCountTo(oldEmail).ShouldBe(oldBefore + 1);
        factory.Emails.LastBodySentTo(oldEmail).ShouldContain(newEmail);
    }

    [Fact]
    public async Task Confirm_RightCode_KeepsThisSessionAndClosesTheOthers()
    {
        (HttpClient client, string email, SessionResponse thisDevice) = await SignedInAsync();
        using HttpClient _ = client;
        using HttpClient anonymous = factory.CreateClient();
        SessionResponse otherDevice = await LoginAsync(anonymous, email);
        string code = await RequestChangeAsync(client, NewEmail());

        await client.PostAsJsonAsync(s_confirmChange, new { code, refreshToken = thisDevice.RefreshToken });

        (await anonymous.PostAsJsonAsync(s_refresh, new { refreshToken = thisDevice.RefreshToken }))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
        (await anonymous.PostAsJsonAsync(s_refresh, new { refreshToken = otherDevice.RefreshToken }))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_WrongPassword_Returns422AndSendsNothing()
    {
        (HttpClient client, _, _) = await SignedInAsync();
        using HttpClient _ = client;
        string newEmail = NewEmail();

        HttpResponseMessage response =
            await client.PostAsJsonAsync(s_request, new { newEmail, password = "Equivocada1" });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await FirstErrorCodeAsync(response)).ShouldBe("Identity.CurrentPasswordIncorrect");
        factory.Emails.SentCountTo(newEmail).ShouldBe(0);
    }

    [Fact]
    public async Task Request_EmailOfAnotherAccount_Returns422()
    {
        (HttpClient client, _, _) = await SignedInAsync();
        using HttpClient _ = client;
        using HttpClient anonymous = factory.CreateClient();
        string taken = await ConfirmedAccountAsync(anonymous);

        HttpResponseMessage response = await client.PostAsJsonAsync(s_request, new { newEmail = taken, password = Password });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await FirstErrorCodeAsync(response)).ShouldBe("Identity.EmailAlreadyRegistered");
    }

    [Fact]
    public async Task Request_SameEmailAsTheCurrentOne_Returns422()
    {
        (HttpClient client, string email, _) = await SignedInAsync();
        using HttpClient _ = client;

        HttpResponseMessage response =
            await client.PostAsJsonAsync(s_request, new { newEmail = email.ToUpperInvariant(), password = Password });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await FirstErrorCodeAsync(response)).ShouldBe("Identity.EmailUnchanged");
    }

    [Fact]
    public async Task Request_MalformedEmail_Returns400()
    {
        (HttpClient client, _, _) = await SignedInAsync();
        using HttpClient _ = client;

        HttpResponseMessage response = await client.PostAsJsonAsync(s_request, new { newEmail = "ana", password = Password });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await FirstErrorCodeAsync(response)).ShouldBe("Request.InvalidValue");
    }

    [Fact]
    public async Task Request_EmailProviderDown_Returns503AndKeepsTheOldEmail()
    {
        (HttpClient client, string email, _) = await SignedInAsync();
        using HttpClient _ = client;
        factory.Emails.FailWith = new Waggo.Domain.Exceptions.ExternalServiceException(
            Waggo.Domain.Errors.Identity.EmailErrors.SendFailed, "ses down");
        try
        {
            HttpResponseMessage response =
                await client.PostAsJsonAsync(s_request, new { newEmail = NewEmail(), password = Password });

            response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            (await FirstErrorCodeAsync(response)).ShouldBe("Email.SendFailed");
        }
        finally
        {
            factory.Emails.FailWith = null;
        }

        UserProfileResponse profile =
            (await (await client.GetAsync(s_me)).Content.ReadFromJsonAsync<WaggoApiResponse<UserProfileResponse>>())!.Data!;
        profile.Email.ShouldBe(email);
    }

    [Fact]
    public async Task Confirm_WrongCode_Returns404AndKeepsTheEmail()
    {
        (HttpClient client, string email, _) = await SignedInAsync();
        using HttpClient _ = client;
        string code = await RequestChangeAsync(client, NewEmail());
        string wrong = code == "000000" ? "000001" : "000000";

        HttpResponseMessage response = await client.PostAsJsonAsync(s_confirmChange, new { code = wrong });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await FirstErrorCodeAsync(response)).ShouldBe("Identity.EmailChangeCodeInvalid");
        UserProfileResponse profile =
            (await (await client.GetAsync(s_me)).Content.ReadFromJsonAsync<WaggoApiResponse<UserProfileResponse>>())!.Data!;
        profile.Email.ShouldBe(email);
    }

    [Fact]
    public async Task Confirm_FiveWrongCodes_BlockTheRightOne()
    {
        (HttpClient client, _, _) = await SignedInAsync();
        using HttpClient _ = client;
        string code = await RequestChangeAsync(client, NewEmail());
        string wrong = code == "000000" ? "000001" : "000000";
        for (int attempt = 0; attempt < User.MaxEmailChangeAttempts; attempt++)
        {
            await client.PostAsJsonAsync(s_confirmChange, new { code = wrong });
        }

        HttpResponseMessage blocked = await client.PostAsJsonAsync(s_confirmChange, new { code });

        blocked.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await FirstErrorCodeAsync(blocked)).ShouldBe("Identity.EmailChangeAttemptsExceeded");
    }

    [Fact]
    public async Task Confirm_AfterTheCodeExpired_Returns409()
    {
        (HttpClient client, string email, _) = await SignedInAsync();
        using HttpClient _ = client;
        string code = await RequestChangeAsync(client, NewEmail());
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            WaggoDbContext db = scope.ServiceProvider.GetRequiredService<WaggoDbContext>();
            DateTimeOffset past = DateTimeOffset.UtcNow.AddMinutes(-1);
            await db.Users.Where(user => user.Email == email)
                .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.EmailChangeCodeExpiresAt, past));
        }

        HttpResponseMessage response = await client.PostAsJsonAsync(s_confirmChange, new { code });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await FirstErrorCodeAsync(response)).ShouldBe("Identity.EmailChangeCodeExpired");
    }

    [Fact]
    public async Task Request_ASecondRequestReplacesTheFirstOne()
    {
        (HttpClient client, _, _) = await SignedInAsync();
        using HttpClient _ = client;
        string firstEmail = NewEmail();
        string secondEmail = NewEmail();
        string firstCode = await RequestChangeAsync(client, firstEmail);
        string secondCode = await RequestChangeAsync(client, secondEmail);

        if (firstCode != secondCode)
        {
            (await client.PostAsJsonAsync(s_confirmChange, new { code = firstCode }))
                .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        (await client.PostAsJsonAsync(s_confirmChange, new { code = secondCode })).StatusCode.ShouldBe(HttpStatusCode.OK);
        UserProfileResponse profile =
            (await (await client.GetAsync(s_me)).Content.ReadFromJsonAsync<WaggoApiResponse<UserProfileResponse>>())!.Data!;
        profile.Email.ShouldBe(secondEmail);
    }

    [Fact]
    public async Task Confirm_TheNewEmailWasTakenMeanwhile_Returns422()
    {
        (HttpClient client, _, _) = await SignedInAsync();
        using HttpClient _ = client;
        using HttpClient anonymous = factory.CreateClient();
        string newEmail = NewEmail();
        string code = await RequestChangeAsync(client, newEmail);
        await anonymous.PostAsJsonAsync(s_register, new { email = newEmail, password = Password });

        HttpResponseMessage response = await client.PostAsJsonAsync(s_confirmChange, new { code });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await FirstErrorCodeAsync(response)).ShouldBe("Identity.EmailAlreadyRegistered");
    }

    [Fact]
    public async Task Both_WithoutASession_Return401()
    {
        using HttpClient client = factory.CreateClient();

        (await client.PostAsJsonAsync(s_request, new { newEmail = NewEmail(), password = Password }))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync(s_confirmChange, new { code = "123456" }))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
