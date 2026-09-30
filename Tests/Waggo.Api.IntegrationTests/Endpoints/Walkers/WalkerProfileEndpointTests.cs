using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Walkers;
using Waggo.Infrastructure.Persistence.Context;

namespace Waggo.Api.IntegrationTests.Endpoints.Walkers;

/// <summary>Acceptance tests for RF-002 and RF-003: walkers register and an admin verifies them.</summary>
[Collection(ApiCollectionDefinition.Name)]
public class WalkerProfileEndpointTests(WaggoApiFactory factory)
{
    private static readonly Uri s_me = new("/api/v1/walkers/me", UriKind.Relative);

    [Fact]
    public async Task Register_ValidProfile_IsPendingAndShowsOnlyTheLast4Digits()
    {
        using HttpClient walker = WalkScenario.Client(factory, "walker");

        HttpResponseMessage response = await walker.PostAsJsonAsync(s_me, WalkScenario.WalkerProfileRequest());
        WalkerProfileResponse mine = (await walker.GetFromJsonAsync<WaggoApiResponse<WalkerProfileResponse>>(s_me))!.Data!;

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (mine.FullName, mine.DocumentType, mine.DocumentLast4, mine.Status)
            .ShouldBe(("Andrés Gómez", "CC", "4050", "Pending"));
    }

    [Fact]
    public async Task Register_Twice_Returns409()
    {
        using HttpClient walker = WalkScenario.Client(factory, "walker");
        await walker.PostAsJsonAsync(s_me, WalkScenario.WalkerProfileRequest());

        HttpResponseMessage response = await walker.PostAsJsonAsync(s_me, WalkScenario.WalkerProfileRequest());

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        await WalkScenario.ShouldHaveSingleErrorAsync(response, "Walkers.AlreadyRegistered");
    }

    [Fact]
    public async Task Register_InvalidPhone_Returns400()
    {
        using HttpClient walker = WalkScenario.Client(factory, "walker");

        HttpResponseMessage response =
            await walker.PostAsJsonAsync(s_me, WalkScenario.WalkerProfileRequest(phone: "30012"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await WalkScenario.ShouldHaveSingleErrorAsync(response, "Walkers.InvalidPhone");
    }

    [Fact]
    public async Task Register_TheDocumentNumberIsEncryptedInTheDatabase_Rnf003()
    {
        using HttpClient walker = WalkScenario.Client(factory, "walker");
        HttpResponseMessage response = await walker.PostAsJsonAsync(s_me, WalkScenario.WalkerProfileRequest());
        Guid id = (await response.Content.ReadFromJsonAsync<WaggoApiResponse<WalkerProfileResponse>>())!.Data!.Id;

        using IServiceScope scope = factory.Services.CreateScope();
        string stored = await scope.ServiceProvider.GetRequiredService<WaggoDbContext>().Database
            .SqlQuery<string>($"SELECT document_number AS \"Value\" FROM identity.walker_profiles WHERE id = {id}")
            .SingleAsync();

        stored.ShouldNotContain("1020304050");
    }

    [Fact]
    public async Task Admin_ListsPendingAndApproves_ThenTheWalkerSeesRequests()
    {
        using HttpClient walker = WalkScenario.Client(factory, "walker");
        using HttpClient admin = WalkScenario.Client(factory, "admin");
        HttpResponseMessage registered = await walker.PostAsJsonAsync(s_me, WalkScenario.WalkerProfileRequest());
        Guid id = (await registered.Content.ReadFromJsonAsync<WaggoApiResponse<WalkerProfileResponse>>())!.Data!.Id;

        HttpResponseMessage beforeApproval = await walker.GetAsync(Available());
        List<WalkerProfileResponse> pending = (await admin.GetFromJsonAsync<WaggoApiResponse<List<WalkerProfileResponse>>>(
            new Uri("/api/v1/admin/walkers?status=Pending", UriKind.Relative)))!.Data!;
        HttpResponseMessage approved = await admin.PostAsync(AdminUri(id, "approve"), content: null);
        HttpResponseMessage afterApproval = await walker.GetAsync(Available());

        beforeApproval.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        await WalkScenario.ShouldHaveSingleErrorAsync(beforeApproval, "Walkers.NotVerified");
        pending.ShouldContain(profile => profile.Id == id);
        (await approved.Content.ReadFromJsonAsync<WaggoApiResponse<WalkerProfileResponse>>())!.Data!.Status
            .ShouldBe("Approved");
        afterApproval.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Admin_Rejects_WithAReason()
    {
        using HttpClient walker = WalkScenario.Client(factory, "walker");
        using HttpClient admin = WalkScenario.Client(factory, "admin");
        HttpResponseMessage registered = await walker.PostAsJsonAsync(s_me, WalkScenario.WalkerProfileRequest());
        Guid id = (await registered.Content.ReadFromJsonAsync<WaggoApiResponse<WalkerProfileResponse>>())!.Data!.Id;

        HttpResponseMessage rejected =
            await admin.PostAsJsonAsync(AdminUri(id, "reject"), new { reason = "Documento ilegible" });

        WalkerProfileResponse profile =
            (await rejected.Content.ReadFromJsonAsync<WaggoApiResponse<WalkerProfileResponse>>())!.Data!;
        (profile.Status, profile.RejectionReason).ShouldBe(("Rejected", "Documento ilegible"));
    }

    [Fact]
    public async Task NotVerifiedWalker_CannotAccept()
    {
        using HttpClient owner = WalkScenario.Client(factory, "owner");
        using HttpClient walker = WalkScenario.Client(factory, "walker");
        await walker.PostAsJsonAsync(s_me, WalkScenario.WalkerProfileRequest());
        Application.Walks.WalkResponse walk = await WalkScenario.RequestAsync(owner);

        HttpResponseMessage response = await walker.PostAsync(WalkScenario.WalkUri(walk.Id, "accept"), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        await WalkScenario.ShouldHaveSingleErrorAsync(response, "Walkers.NotVerified");
    }

    [Fact]
    public async Task Walker_CannotApprove()
    {
        using HttpClient walker = WalkScenario.Client(factory, "walker");

        HttpResponseMessage response = await walker.PostAsync(AdminUri(Guid.NewGuid(), "approve"), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static Uri Available() => new("/api/v1/walks/available", UriKind.Relative);

    private static Uri AdminUri(Guid profileId, string action) =>
        new($"/api/v1/admin/walkers/{profileId}/{action}", UriKind.Relative);
}
