using System.Net;
using System.Net.Http.Json;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Identity;
using Waggo.Domain.Errors.Identity;
using Waggo.Domain.Exceptions;

namespace Waggo.Api.IntegrationTests.Endpoints.Identity;

/// <summary>Acceptance tests for RF-001: create an account and confirm it by email. AllowAnonymous end to end.</summary>
[Collection(ApiCollectionDefinition.Name)]
public class IdentityEndpointTests(WaggoApiFactory factory)
{
    private static readonly Uri s_register = new("/api/v1/auth/register", UriKind.Relative);

    [Fact]
    public async Task Register_NewEmail_Returns200WithTheAccount()
    {
        using HttpClient client = factory.CreateClient();
        string email = $"ana-{Guid.NewGuid():N}@mail.com";

        HttpResponseMessage response =
            await client.PostAsJsonAsync(s_register, new { email, password = "Clave123!" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        UserResponse user = (await response.Content.ReadFromJsonAsync<WaggoApiResponse<UserResponse>>())!.Data!;
        user.Email.ShouldBe(email);
    }

    [Fact]
    public async Task Register_EmailAlreadyRegistered_Returns422()
    {
        using HttpClient client = factory.CreateClient();
        string email = $"ana-{Guid.NewGuid():N}@mail.com";
        await client.PostAsJsonAsync(s_register, new { email, password = "Clave123!" });

        HttpResponseMessage response =
            await client.PostAsJsonAsync(s_register, new { email, password = "OtraClave456!" });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        WaggoApiResponse<object>? body = await response.Content.ReadFromJsonAsync<WaggoApiResponse<object>>();
        body!.Errors.Single().Code.ShouldBe("Identity.EmailAlreadyRegistered");
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            s_register,
            new { email = $"ana-{Guid.NewGuid():N}@mail.com", password = "short1" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_EmailProviderDown_Returns503AndLeavesTheEmailFreeToTryAgain()
    {
        using HttpClient client = factory.CreateClient();
        string email = $"ana-{Guid.NewGuid():N}@mail.com";
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
    public async Task Confirm_TokenFromTheConfirmationEmail_ActivatesTheAccount()
    {
        using HttpClient client = factory.CreateClient();
        string email = $"ana-{Guid.NewGuid():N}@mail.com";
        await client.PostAsJsonAsync(s_register, new { email, password = "Clave123!" });
        string token = ExtractToken(factory.Emails.LastBodySentTo(email));

        HttpResponseMessage response = await client.GetAsync(new Uri($"/api/v1/auth/confirm?token={token}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<WaggoApiResponse<bool>>())!.Data.ShouldBeTrue();
    }

    [Fact]
    public async Task Confirm_UnknownToken_Returns404()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response =
            await client.GetAsync(new Uri("/api/v1/auth/confirm?token=does-not-exist", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static string ExtractToken(string emailBody) => emailBody.Split(": ")[^1];
}
