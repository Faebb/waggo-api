using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Identity;
using Waggo.Domain.Entities.Identity;
using Waggo.Domain.Errors.Identity;
using Waggo.Domain.Exceptions;
using Waggo.Infrastructure.Persistence.Context;

namespace Waggo.Api.IntegrationTests.Endpoints.Identity;

/// <summary>
/// Acceptance tests for RF-001: create an account and confirm it with the 6-digit code emailed to the owner of the
/// inbox. AllowAnonymous end to end.
/// </summary>
[Collection(ApiCollectionDefinition.Name)]
public class IdentityEndpointTests(WaggoApiFactory factory)
{
    private static readonly Uri s_register = new("/api/v1/auth/register", UriKind.Relative);
    private static readonly Uri s_confirm = new("/api/v1/auth/confirm", UriKind.Relative);
    private static readonly Uri s_resend = new("/api/v1/auth/resend-confirmation", UriKind.Relative);

    private static string NewEmail() => $"ana-{Guid.NewGuid():N}@mail.com";

    private static async Task<string> FirstErrorCodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<WaggoApiResponse<object>>())!.Errors.Single().Code;

    private static async Task<string> RegisterAsync(HttpClient client)
    {
        string email = NewEmail();
        await client.PostAsJsonAsync(s_register, new { email, password = "Clave123!" });
        return email;
    }

    /// <summary>Moves the code's sent time back, so the 60 s wait before a resend has already passed.</summary>
    private async Task LetTheResendWaitPassAsync(string email)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        WaggoDbContext db = scope.ServiceProvider.GetRequiredService<WaggoDbContext>();
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.Add(User.ConfirmationTokenLifetime)
            - User.ConfirmationResendCooldown - TimeSpan.FromMinutes(1);
        await db.Users.Where(user => user.Email == email)
            .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.ConfirmationTokenExpiresAt, expiresAt));
    }

    [Fact]
    public async Task Register_NewEmail_Returns200WithTheAccount()
    {
        using HttpClient client = factory.CreateClient();
        string email = NewEmail();

        HttpResponseMessage response =
            await client.PostAsJsonAsync(s_register, new { email, password = "Clave123!" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        UserResponse user = (await response.Content.ReadFromJsonAsync<WaggoApiResponse<UserResponse>>())!.Data!;
        user.Email.ShouldBe(email);
        factory.Emails.LastConfirmationCodeSentTo(email).Length.ShouldBe(6);
    }

    [Fact]
    public async Task Register_EmailAlreadyRegistered_Returns422()
    {
        using HttpClient client = factory.CreateClient();
        string email = await RegisterAsync(client);

        HttpResponseMessage response =
            await client.PostAsJsonAsync(s_register, new { email, password = "OtraClave456!" });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await FirstErrorCodeAsync(response)).ShouldBe("Identity.EmailAlreadyRegistered");
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            s_register,
            new { email = NewEmail(), password = "short1" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_EmailProviderDown_Returns503AndLeavesTheEmailFreeToTryAgain()
    {
        using HttpClient client = factory.CreateClient();
        string email = NewEmail();
        factory.Emails.FailWith = new ExternalServiceException(EmailErrors.SendFailed, "SES is down.");

        HttpResponseMessage failed;
        try
        {
            failed = await client.PostAsJsonAsync(s_register, new { email, password = "Clave123!" });
        }
        finally
        {
            factory.Emails.FailWith = null;
        }

        failed.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        WaggoApiResponse<object>? body = await failed.Content.ReadFromJsonAsync<WaggoApiResponse<object>>();
        body!.Errors.Single().Code.ShouldBe("Email.SendFailed");
        body.Errors.Single().Message.ShouldNotContain("SES");

        HttpResponseMessage retry = await client.PostAsJsonAsync(s_register, new { email, password = "Clave123!" });
        retry.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Confirm_CodeFromTheConfirmationEmail_ActivatesTheAccount()
    {
        using HttpClient client = factory.CreateClient();
        string email = await RegisterAsync(client);
        string code = factory.Emails.LastConfirmationCodeSentTo(email);

        HttpResponseMessage response = await client.PostAsJsonAsync(s_confirm, new { email, code });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<WaggoApiResponse<bool>>())!.Data.ShouldBeTrue();
    }

    [Fact]
    public async Task Confirm_WrongCode_Returns404WithInvalidCode()
    {
        using HttpClient client = factory.CreateClient();
        string email = await RegisterAsync(client);
        string code = factory.Emails.LastConfirmationCodeSentTo(email);
        string wrong = code == "000000" ? "000001" : "000000";

        HttpResponseMessage response = await client.PostAsJsonAsync(s_confirm, new { email, code = wrong });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await FirstErrorCodeAsync(response)).ShouldBe("Identity.ConfirmationTokenInvalid");
    }

    [Fact]
    public async Task Confirm_EmailWithoutAnAccount_Returns404WithTheSameError()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response =
            await client.PostAsJsonAsync(s_confirm, new { email = NewEmail(), code = "123456" });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await FirstErrorCodeAsync(response)).ShouldBe("Identity.ConfirmationTokenInvalid");
    }

    [Fact]
    public async Task Confirm_CodeThatIsNotSixDigits_Returns400()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response =
            await client.PostAsJsonAsync(s_confirm, new { email = NewEmail(), code = "12ab" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await FirstErrorCodeAsync(response)).ShouldBe("Request.InvalidValue");
    }

    [Fact]
    public async Task Confirm_FiveWrongCodes_BlockTheRightOneUntilANewCodeIsRequested()
    {
        using HttpClient client = factory.CreateClient();
        string email = await RegisterAsync(client);
        string code = factory.Emails.LastConfirmationCodeSentTo(email);
        string wrong = code == "000000" ? "000001" : "000000";
        for (int attempt = 0; attempt < User.MaxConfirmationAttempts; attempt++)
        {
            await client.PostAsJsonAsync(s_confirm, new { email, code = wrong });
        }

        HttpResponseMessage blocked = await client.PostAsJsonAsync(s_confirm, new { email, code });

        blocked.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await FirstErrorCodeAsync(blocked)).ShouldBe("Identity.ConfirmationAttemptsExceeded");

        await LetTheResendWaitPassAsync(email);
        await client.PostAsJsonAsync(s_resend, new { email });
        string newCode = factory.Emails.LastConfirmationCodeSentTo(email);
        HttpResponseMessage confirmed = await client.PostAsJsonAsync(s_confirm, new { email, code = newCode });
        confirmed.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResendConfirmation_AfterTheWaitTime_SendsANewCodeEmail()
    {
        using HttpClient client = factory.CreateClient();
        string email = await RegisterAsync(client);
        await LetTheResendWaitPassAsync(email);

        HttpResponseMessage response = await client.PostAsJsonAsync(s_resend, new { email });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<WaggoApiResponse<bool>>())!.Data.ShouldBeTrue();
        factory.Emails.SentCountTo(email).ShouldBe(2);
    }

    [Fact]
    public async Task ResendConfirmation_BeforeTheWaitTime_AnswersOkWithoutSendingAgain()
    {
        using HttpClient client = factory.CreateClient();
        string email = await RegisterAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync(s_resend, new { email });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        factory.Emails.SentCountTo(email).ShouldBe(1);
    }

    [Fact]
    public async Task ResendConfirmation_EmailWithoutAnAccount_AnswersTheSameAndSendsNothing()
    {
        using HttpClient client = factory.CreateClient();
        string email = NewEmail();

        HttpResponseMessage response = await client.PostAsJsonAsync(s_resend, new { email });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<WaggoApiResponse<bool>>())!.Data.ShouldBeTrue();
        factory.Emails.SentCountTo(email).ShouldBe(0);
    }

    [Fact]
    public async Task ResendConfirmation_MalformedEmail_Returns400()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(s_resend, new { email = "not-an-email" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
