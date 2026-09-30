using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Pets;
using Waggo.Infrastructure.Persistence.Context;

namespace Waggo.Api.IntegrationTests.Endpoints.Pets;

/// <summary>Acceptance tests for RF-004: the owner registers their dogs and only sees their own.</summary>
[Collection(ApiCollectionDefinition.Name)]
public class PetsEndpointTests(WaggoApiFactory factory)
{
    private static readonly Uri s_pets = new("/api/v1/pets", UriKind.Relative);

    private static readonly object s_luna = new
    {
        name = "Luna",
        breed = "Criolla",
        size = "Medium",
        birthDate = "2021-05-10",
        weightKg = 14.5m,
        medicalNotes = "Alérgica al pollo",
    };

    [Fact]
    public async Task Post_FullPet_Returns200WithThePet()
    {
        using HttpClient client = ClientForNewOwner();

        HttpResponseMessage response = await client.PostAsJsonAsync(s_pets, s_luna);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        WaggoApiResponse<PetResponse>? body = await response.Content.ReadFromJsonAsync<WaggoApiResponse<PetResponse>>();
        body!.Success.ShouldBeTrue();
        body.Data!.Id.ShouldNotBe(Guid.Empty);
        body.Data.ShouldBe(body.Data with
        {
            Name = "Luna",
            Breed = "Criolla",
            Size = "Medium",
            BirthDate = new DateOnly(2021, 5, 10),
            WeightKg = 14.5m,
            MedicalNotes = "Alérgica al pollo",
        });
    }

    [Fact]
    public async Task Post_OnlyRequiredFields_LeavesOptionalFieldsNull()
    {
        using HttpClient client = ClientForNewOwner();

        HttpResponseMessage response = await client.PostAsJsonAsync(s_pets, new { name = "Max", size = "Small" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        PetResponse pet = (await response.Content.ReadFromJsonAsync<WaggoApiResponse<PetResponse>>())!.Data!;
        pet.Name.ShouldBe("Max");
        pet.Size.ShouldBe("Small");
        pet.Breed.ShouldBeNull();
        pet.BirthDate.ShouldBeNull();
        pet.WeightKg.ShouldBeNull();
        pet.MedicalNotes.ShouldBeNull();
    }

    [Fact]
    public async Task Get_ReturnsOnlyTheOwnersPetsSortedByName()
    {
        using HttpClient owner = ClientForNewOwner();
        using HttpClient otherOwner = ClientForNewOwner();
        await owner.PostAsJsonAsync(s_pets, new { name = "Max", size = "Small" });
        await owner.PostAsJsonAsync(s_pets, s_luna);

        WaggoApiResponse<List<PetResponse>>? mine =
            await owner.GetFromJsonAsync<WaggoApiResponse<List<PetResponse>>>(s_pets);
        WaggoApiResponse<List<PetResponse>>? others =
            await otherOwner.GetFromJsonAsync<WaggoApiResponse<List<PetResponse>>>(s_pets);

        mine!.Data!.Select(pet => pet.Name).ShouldBe(["Luna", "Max"]);
        others!.Data.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetById_OwnPet_ReturnsTheDecryptedMedicalNotes()
    {
        using HttpClient client = ClientForNewOwner();
        PetResponse created = await RegisterAsync(client, s_luna);

        WaggoApiResponse<PetResponse>? body =
            await client.GetFromJsonAsync<WaggoApiResponse<PetResponse>>(PetUri(created.Id));

        body!.Data.ShouldBe(created);
        body.Data!.MedicalNotes.ShouldBe("Alérgica al pollo");
    }

    [Fact]
    public async Task GetById_PetOfAnotherOwner_Returns404()
    {
        using HttpClient owner = ClientForNewOwner();
        using HttpClient otherOwner = ClientForNewOwner();
        PetResponse created = await RegisterAsync(owner, s_luna);

        HttpResponseMessage response = await otherOwner.GetAsync(PetUri(created.Id));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        await ShouldHaveSingleErrorAsync(response, "Pets.NotFound");
    }

    [Fact]
    public async Task GetById_UnknownPet_Returns404()
    {
        using HttpClient client = ClientForNewOwner();

        HttpResponseMessage response = await client.GetAsync(PetUri(Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        await ShouldHaveSingleErrorAsync(response, "Pets.NotFound");
    }

    [Fact]
    public async Task Post_MedicalNotes_AreEncryptedInTheDatabase_Rnf003()
    {
        using HttpClient client = ClientForNewOwner();
        PetResponse created = await RegisterAsync(client, s_luna);

        using IServiceScope scope = factory.Services.CreateScope();
        WaggoDbContext db = scope.ServiceProvider.GetRequiredService<WaggoDbContext>();
        string stored = await db.Database
            .SqlQuery<string>($"SELECT medical_notes AS \"Value\" FROM pets.pets WHERE id = {created.Id}")
            .SingleAsync();

        stored.ShouldNotBeNullOrWhiteSpace();
        stored.ShouldNotContain("pollo");
    }

    [Fact]
    public async Task Post_WithoutName_FailsTheDtoValidation()
    {
        using HttpClient client = ClientForNewOwner();

        HttpResponseMessage response = await client.PostAsJsonAsync(s_pets, new { size = "Small" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await ShouldHaveSingleErrorAsync(response, "Request.Required");
    }

    [Fact]
    public async Task Post_UnknownSize_FailsTheDtoValidation()
    {
        using HttpClient client = ClientForNewOwner();

        HttpResponseMessage response = await client.PostAsJsonAsync(s_pets, new { name = "Max", size = "Giant" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await ShouldHaveSingleErrorAsync(response, "Request.InvalidValue");
    }

    [Fact]
    public async Task Post_WeightOutOfRange_Returns400WithBusinessCode()
    {
        using HttpClient client = ClientForNewOwner();

        HttpResponseMessage response =
            await client.PostAsJsonAsync(s_pets, new { name = "Max", size = "Small", weightKg = 120 });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await ShouldHaveSingleErrorAsync(response, "Pets.InvalidWeight");
    }

    [Fact]
    public async Task Post_EleventhPet_Returns422LimitReached()
    {
        using HttpClient client = ClientForNewOwner();
        for (int i = 1; i <= 10; i++)
        {
            await RegisterAsync(client, new { name = $"Perro {i}", size = "Small" });
        }

        HttpResponseMessage response = await client.PostAsJsonAsync(s_pets, new { name = "Once", size = "Small" });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        await ShouldHaveSingleErrorAsync(response, "Pets.LimitReached");
    }

    [Fact]
    public async Task Post_AsWalker_Returns403()
    {
        using HttpClient client = ClientForNewOwner();
        client.DefaultRequestHeaders.Add("X-Dev-Roles", "walker");

        HttpResponseMessage response = await client.PostAsJsonAsync(s_pets, s_luna);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        await ShouldHaveSingleErrorAsync(response, "Auth.Forbidden");
    }

    /// <summary>Each test signs in as a different owner, so tests never see each other's pets.</summary>
    private HttpClient ClientForNewOwner()
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User-Id", $"owner-{Guid.NewGuid():N}");
        return client;
    }

    private static Uri PetUri(Guid id) => new($"/api/v1/pets/{id}", UriKind.Relative);

    private static async Task<PetResponse> RegisterAsync(HttpClient client, object pet)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(s_pets, pet);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<WaggoApiResponse<PetResponse>>())!.Data!;
    }

    private static async Task ShouldHaveSingleErrorAsync(HttpResponseMessage response, string code)
    {
        WaggoApiResponse<object>? body = await response.Content.ReadFromJsonAsync<WaggoApiResponse<object>>();
        body!.Success.ShouldBeFalse();
        body.Errors.Single().Code.ShouldBe(code);
    }
}
