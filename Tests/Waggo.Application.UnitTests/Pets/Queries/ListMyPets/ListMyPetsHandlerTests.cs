using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Pets;
using Waggo.Application.Pets;
using Waggo.Application.Pets.Queries.ListMyPets;
using Waggo.Application.UnitTests.TestData;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Pets;

namespace Waggo.Application.UnitTests.Pets.Queries.ListMyPets;

public class ListMyPetsHandlerTests
{
    private readonly IPetRepository _pets = Substitute.For<IPetRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    [Fact]
    public async Task HandleAsync_ReturnsTheCurrentOwnersPets()
    {
        _currentUser.Id.Returns("owner-1");
        IReadOnlyList<Pet> stored = [PetMother.Luna(), PetMother.Max()];
        _pets.ListByOwnerAsync("owner-1", Arg.Any<CancellationToken>()).Returns(stored);
        ListMyPetsHandler sut = new(_pets, _currentUser);

        WaggoResponse<IReadOnlyList<PetResponse>> result =
            await sut.HandleAsync(new ListMyPetsQuery(), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.Select(pet => pet.Name).ShouldBe(["Luna", "Max"]);
    }
}
