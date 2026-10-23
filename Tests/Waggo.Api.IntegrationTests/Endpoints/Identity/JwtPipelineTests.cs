using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Identity;
using Waggo.Application.Pets;
using Waggo.Application.Walkers;
using Waggo.Domain.Entities.Walkers;
using Waggo.Domain.Enums.Walkers;
using Waggo.Infrastructure.Persistence.Context;

namespace Waggo.Api.IntegrationTests.Endpoints.Identity;

/// <summary>
/// With <see cref="JwtPipelineWaggoApiFactory"/> (no development user): a token issued by RF-005 must be what
/// <c>AddWaggoAuthentication</c> accepts for every other endpoint, end to end. No <c>X-Dev-*</c> header is sent:
/// the roles come only from the database (the walker role from an approved walker profile, RF-003).
/// </summary>
[Collection(JwtPipelineCollectionDefinition.Name)]
public class JwtPipelineTests(JwtPipelineWaggoApiFactory factory)
{
    private const string Password = "Clave123!";

    private static readonly Uri s_walkersMe = new("/api/v1/walkers/me", UriKind.Relative);

    /// <summary>Requires the walker policy: probes whether the token carries the walker role.</summary>
    private static readonly Uri s_availableWalks = new("/api/v1/walks/available", UriKind.Relative);

    private static string ExtractCode(string emailBody) => emailBody.Split(": ")[^1];

    private static string NewEmail() => $"ana-{Guid.NewGuid():N}@mail.com";

    private static string UserIdOf(string accessToken) =>
        new JwtSecurityTokenHandler().ReadJwtToken(accessToken).Subject;

    private async Task<string> IssueAccessTokenAsync(HttpClient client) =>
        (await RegisterAndLoginAsync(client, NewEmail())).AccessToken;

    private async Task<SessionResponse> RegisterAndLoginAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync(new Uri("/api/v1/auth/register", UriKind.Relative), new { email, password = Password });
        string confirmationToken = ExtractCode(factory.Emails.LastBodySentTo(email));
        await client.GetAsync(new Uri($"/api/v1/auth/confirm?token={confirmationToken}", UriKind.Relative));

        return await LoginAsync(client, email);
    }

    private async Task<SessionResponse> LoginAsync(HttpClient client, string email)
    {
        HttpResponseMessage login = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative), new { email, password = Password });
        Guid challengeId = (await login.Content.ReadFromJsonAsync<WaggoApiResponse<LoginResponse>>())!.Data!.ChallengeId;
        string code = ExtractCode(factory.Emails.LastBodySentTo(email));

        HttpResponseMessage verify = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login/verify", UriKind.Relative), new { challengeId, code });
        return (await verify.Content.ReadFromJsonAsync<WaggoApiResponse<SessionResponse>>())!.Data!;
    }

    private static async Task<SessionResponse> RefreshAsync(HttpClient client, string refreshToken)
    {
        HttpResponseMessage refreshed = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/refresh", UriKind.Relative), new { refreshToken });
        refreshed.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await refreshed.Content.ReadFromJsonAsync<WaggoApiResponse<SessionResponse>>())!.Data!;
    }

    private static async Task<HttpResponseMessage> GetAvailableWalksAsync(HttpClient client, string accessToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, s_availableWalks);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    /// <summary>Stores the user's walker profile in the test database, as RF-002/RF-003 would leave it.</summary>
    private async Task SeedWalkerProfileAsync(string userId, bool approved)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        WalkerProfile profile = WalkerProfile
            .Register(userId, "Andrés Gómez", DocumentType.CC, "1020304050", "3001234567", null, now).Data!;
        if (approved)
        {
            profile.Approve("admin-1", now).IsValid.ShouldBeTrue();
        }

        using IServiceScope scope = factory.Services.CreateScope();
        WaggoDbContext db = scope.ServiceProvider.GetRequiredService<WaggoDbContext>();
        db.WalkerProfiles.Add(profile);
        await db.SaveChangesAsync();
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

    [Fact]
    public async Task Login_UserWithoutWalkerProfile_GetsNoWalkerRole()
    {
        using HttpClient client = factory.CreateClient();
        SessionResponse session = await RegisterAndLoginAsync(client, NewEmail());

        HttpResponseMessage response = await GetAvailableWalksAsync(client, session.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Refresh_UserWithPendingWalkerProfile_GetsNoWalkerRole()
    {
        using HttpClient client = factory.CreateClient();
        SessionResponse first = await RegisterAndLoginAsync(client, NewEmail());
        await SeedWalkerProfileAsync(UserIdOf(first.AccessToken), approved: false);

        SessionResponse refreshed = await RefreshAsync(client, first.RefreshToken);
        HttpResponseMessage response = await GetAvailableWalksAsync(client, refreshed.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Login_UserWithApprovedWalkerProfile_GetsWalkerRole()
    {
        using HttpClient client = factory.CreateClient();
        string email = NewEmail();
        SessionResponse first = await RegisterAndLoginAsync(client, email);
        await SeedWalkerProfileAsync(UserIdOf(first.AccessToken), approved: true);

        SessionResponse second = await LoginAsync(client, email);
        HttpResponseMessage response = await GetAvailableWalksAsync(client, second.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RegisterWalker_RealUserWithoutWalkerRole_IsAllowedAndPending()
    {
        using HttpClient client = factory.CreateClient();
        string accessToken = await IssueAccessTokenAsync(client);
        using HttpRequestMessage request = new(HttpMethod.Post, s_walkersMe);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new
        {
            fullName = "Andrés Gómez",
            documentType = "CC",
            documentNumber = "1020304050",
            phone = "3001234567",
        });

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<WaggoApiResponse<WalkerProfileResponse>>())!.Data!.Status
            .ShouldBe("Pending");
    }

    [Fact]
    public async Task RegisterWalker_NoToken_Returns401()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(s_walkersMe, new { fullName = "Andrés Gómez" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WalkerApprovedAfterLogin_GetsWalkerRole()
    {
        using HttpClient client = factory.CreateClient();
        SessionResponse first = await RegisterAndLoginAsync(client, NewEmail());
        (await GetAvailableWalksAsync(client, first.AccessToken)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        await SeedWalkerProfileAsync(UserIdOf(first.AccessToken), approved: true);

        SessionResponse refreshed = await RefreshAsync(client, first.RefreshToken);
        HttpResponseMessage response = await GetAvailableWalksAsync(client, refreshed.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
