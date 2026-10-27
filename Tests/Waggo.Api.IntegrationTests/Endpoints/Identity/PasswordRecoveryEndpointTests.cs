using System.Net;
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
/// Acceptance tests for RF-001/RF-005: a forgotten password is recovered with a 6-digit code sent to the inbox.
/// AllowAnonymous end to end.
/// </summary>
[Collection(ApiCollectionDefinition.Name)]
public class PasswordRecoveryEndpointTests(WaggoApiFactory factory)
{
    private const string OldPassword = "Clave123!";
    private const string NewPassword = "Nueva1234";

    private static readonly Uri s_register = new("/api/v1/auth/register", UriKind.Relative);
    private static readonly Uri s_confirm = new("/api/v1/auth/confirm", UriKind.Relative);
    private static readonly Uri s_login = new("/api/v1/auth/login", UriKind.Relative);
    private static readonly Uri s_refresh = new("/api/v1/auth/refresh", UriKind.Relative);
    private static readonly Uri s_forgot = new("/api/v1/auth/forgot-password", UriKind.Relative);
    private static readonly Uri s_reset = new("/api/v1/auth/reset-password", UriKind.Relative);

    private static string NewEmail() => $"ana-{Guid.NewGuid():N}@mail.com";

    private static async Task<string> FirstErrorCodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<WaggoApiResponse<object>>())!.Errors.Single().Code;

    private async Task<string> ConfirmedAccountAsync(HttpClient client)
    {
        string email = NewEmail();
        await client.PostAsJsonAsync(s_register, new { email, password = OldPassword });
        string code = factory.Emails.LastConfirmationCodeSentTo(email);
        await client.PostAsJsonAsync(s_confirm, new { email, code });
        return email;
    }

    /// <summary>Moves the code's sent time back, so the 60 s wait before another code has already passed.</summary>
    private async Task LetTheWaitPassAsync(string email)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        WaggoDbContext db = scope.ServiceProvider.GetRequiredService<WaggoDbContext>();
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.Add(User.PasswordResetCodeLifetime)
            - User.PasswordResetResendCooldown - TimeSpan.FromMinutes(1);
        await db.Users.Where(user => user.Email == email)
            .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.PasswordResetCodeExpiresAt, expiresAt));
    }

    private async Task<string> RequestCodeAsync(HttpClient client, string email)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(s_forgot, new { email });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return factory.Emails.LastConfirmationCodeSentTo(email);
    }

    [Fact]
    public async Task ForgotPassword_ConfirmedAccount_EmailsASixDigitCode()
    {
        using HttpClient client = factory.CreateClient();
        string email = await ConfirmedAccountAsync(client);
        int before = factory.Emails.SentCountTo(email);

        HttpResponseMessage response = await client.PostAsJsonAsync(s_forgot, new { email });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<WaggoApiResponse<bool>>())!.Data.ShouldBeTrue();
        factory.Emails.SentCountTo(email).ShouldBe(before + 1);
        factory.Emails.LastBodySentTo(email).ShouldContain("restablecer");
    }

    [Fact]
    public async Task ForgotPassword_EmailWithoutAnAccount_AnswersTheSameAndSendsNothing()
    {
        using HttpClient client = factory.CreateClient();
        string email = NewEmail();

        HttpResponseMessage response = await client.PostAsJsonAsync(s_forgot, new { email });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<WaggoApiResponse<bool>>())!.Data.ShouldBeTrue();
        factory.Emails.SentCountTo(email).ShouldBe(0);
    }

    [Fact]
    public async Task ForgotPassword_UnconfirmedAccount_SendsNoRecoveryCode()
    {
        using HttpClient client = factory.CreateClient();
        string email = NewEmail();
        await client.PostAsJsonAsync(s_register, new { email, password = OldPassword });
        int before = factory.Emails.SentCountTo(email);

        HttpResponseMessage response = await client.PostAsJsonAsync(s_forgot, new { email });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        factory.Emails.SentCountTo(email).ShouldBe(before);
    }

    [Fact]
    public async Task ForgotPassword_BeforeTheWaitTime_AnswersOkWithoutSendingAgain()
    {
        using HttpClient client = factory.CreateClient();
        string email = await ConfirmedAccountAsync(client);
        await RequestCodeAsync(client, email);
        int sent = factory.Emails.SentCountTo(email);

        HttpResponseMessage response = await client.PostAsJsonAsync(s_forgot, new { email });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        factory.Emails.SentCountTo(email).ShouldBe(sent);
    }

    [Fact]
    public async Task ForgotPassword_MalformedEmail_Returns400()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(s_forgot, new { email = "ana" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await FirstErrorCodeAsync(response)).ShouldBe("Request.InvalidValue");
    }

    [Fact]
    public async Task ForgotPassword_EmailProviderDown_Returns503()
    {
        using HttpClient client = factory.CreateClient();
        string email = await ConfirmedAccountAsync(client);
        factory.Emails.FailWith = new Waggo.Domain.Exceptions.ExternalServiceException(
            Waggo.Domain.Errors.Identity.EmailErrors.SendFailed, "ses down");
        try
        {
            HttpResponseMessage response = await client.PostAsJsonAsync(s_forgot, new { email });

            response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            (await FirstErrorCodeAsync(response)).ShouldBe("Email.SendFailed");
        }
        finally
        {
            factory.Emails.FailWith = null;
        }
    }

    [Fact]
    public async Task ResetPassword_RightCode_LetsYouLogInWithTheNewPasswordOnly()
    {
        using HttpClient client = factory.CreateClient();
        string email = await ConfirmedAccountAsync(client);
        string code = await RequestCodeAsync(client, email);

        HttpResponseMessage reset =
            await client.PostAsJsonAsync(s_reset, new { email, code, newPassword = NewPassword });

        reset.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await reset.Content.ReadFromJsonAsync<WaggoApiResponse<bool>>())!.Data.ShouldBeTrue();
        HttpResponseMessage oldLogin = await client.PostAsJsonAsync(s_login, new { email, password = OldPassword });
        (await FirstErrorCodeAsync(oldLogin)).ShouldBe("Identity.InvalidCredentials");
        HttpResponseMessage newLogin = await client.PostAsJsonAsync(s_login, new { email, password = NewPassword });
        newLogin.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_RightCode_RevokesTheSessionsOpenBefore()
    {
        using HttpClient client = factory.CreateClient();
        string email = await ConfirmedAccountAsync(client);
        HttpResponseMessage login = await client.PostAsJsonAsync(s_login, new { email, password = OldPassword });
        string refreshToken =
            (await login.Content.ReadFromJsonAsync<WaggoApiResponse<SessionResponse>>())!.Data!.RefreshToken;
        string code = await RequestCodeAsync(client, email);
        await client.PostAsJsonAsync(s_reset, new { email, code, newPassword = NewPassword });

        HttpResponseMessage refresh = await client.PostAsJsonAsync(s_refresh, new { refreshToken });

        refresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResetPassword_RightCode_AlsoEmailsASecurityNotice()
    {
        using HttpClient client = factory.CreateClient();
        string email = await ConfirmedAccountAsync(client);
        string code = await RequestCodeAsync(client, email);
        int before = factory.Emails.SentCountTo(email);

        await client.PostAsJsonAsync(s_reset, new { email, code, newPassword = NewPassword });

        factory.Emails.SentCountTo(email).ShouldBe(before + 1);
        factory.Emails.LastBodySentTo(email).ShouldContain("cambió");
    }

    [Fact]
    public async Task ResetPassword_CodeCannotBeUsedTwice()
    {
        using HttpClient client = factory.CreateClient();
        string email = await ConfirmedAccountAsync(client);
        string code = await RequestCodeAsync(client, email);
        await client.PostAsJsonAsync(s_reset, new { email, code, newPassword = NewPassword });

        HttpResponseMessage again =
            await client.PostAsJsonAsync(s_reset, new { email, code, newPassword = "Otra12345" });

        again.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await FirstErrorCodeAsync(again)).ShouldBe("Identity.ResetCodeInvalid");
    }

    [Fact]
    public async Task ResetPassword_WrongCode_Returns404AndKeepsThePassword()
    {
        using HttpClient client = factory.CreateClient();
        string email = await ConfirmedAccountAsync(client);
        string code = await RequestCodeAsync(client, email);
        string wrong = code == "000000" ? "000001" : "000000";

        HttpResponseMessage response =
            await client.PostAsJsonAsync(s_reset, new { email, code = wrong, newPassword = NewPassword });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await FirstErrorCodeAsync(response)).ShouldBe("Identity.ResetCodeInvalid");
        HttpResponseMessage login = await client.PostAsJsonAsync(s_login, new { email, password = OldPassword });
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_EmailWithoutAnAccount_Returns404WithTheSameError()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            s_reset, new { email = NewEmail(), code = "123456", newPassword = NewPassword });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await FirstErrorCodeAsync(response)).ShouldBe("Identity.ResetCodeInvalid");
    }

    [Fact]
    public async Task ResetPassword_FiveWrongCodes_BlockTheRightOneUntilANewCodeIsRequested()
    {
        using HttpClient client = factory.CreateClient();
        string email = await ConfirmedAccountAsync(client);
        string code = await RequestCodeAsync(client, email);
        string wrong = code == "000000" ? "000001" : "000000";
        for (int attempt = 0; attempt < User.MaxPasswordResetAttempts; attempt++)
        {
            await client.PostAsJsonAsync(s_reset, new { email, code = wrong, newPassword = NewPassword });
        }

        HttpResponseMessage blocked =
            await client.PostAsJsonAsync(s_reset, new { email, code, newPassword = NewPassword });

        blocked.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await FirstErrorCodeAsync(blocked)).ShouldBe("Identity.ResetAttemptsExceeded");

        await LetTheWaitPassAsync(email);
        string newCode = await RequestCodeAsync(client, email);
        HttpResponseMessage reset =
            await client.PostAsJsonAsync(s_reset, new { email, code = newCode, newPassword = NewPassword });
        reset.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_SecondRequestMakesTheFirstCodeUseless()
    {
        using HttpClient client = factory.CreateClient();
        string email = await ConfirmedAccountAsync(client);
        string first = await RequestCodeAsync(client, email);
        await LetTheWaitPassAsync(email);
        string second = await RequestCodeAsync(client, email);

        if (first != second)
        {
            HttpResponseMessage old =
                await client.PostAsJsonAsync(s_reset, new { email, code = first, newPassword = NewPassword });
            old.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        HttpResponseMessage current =
            await client.PostAsJsonAsync(s_reset, new { email, code = second, newPassword = NewPassword });
        current.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_WeakNewPassword_Returns400AndKeepsTheCodeUsable()
    {
        using HttpClient client = factory.CreateClient();
        string email = await ConfirmedAccountAsync(client);
        string code = await RequestCodeAsync(client, email);

        HttpResponseMessage weak =
            await client.PostAsJsonAsync(s_reset, new { email, code, newPassword = "corta" });

        weak.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await FirstErrorCodeAsync(weak)).ShouldBe("Identity.WeakPassword");
        HttpResponseMessage fine =
            await client.PostAsJsonAsync(s_reset, new { email, code, newPassword = NewPassword });
        fine.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_CodeThatIsNotSixDigits_Returns400()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            s_reset, new { email = NewEmail(), code = "12ab", newPassword = NewPassword });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await FirstErrorCodeAsync(response)).ShouldBe("Request.InvalidValue");
    }
}
