using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Pets;
using Waggo.Application.Pets;
using Waggo.Application.Pets.Queries.GetPet;
using Waggo.Application.UnitTests.TestData;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Pets;
using Waggo.Domain.Errors.Pets;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.UnitTests.Pets.Queries.GetPet;

public class GetPetHandlerTests
{
    private readonly IPetRepository _pets = Substitute.For<IPetRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly GetPetHandler _sut;

    public GetPetHandlerTests()
    {
        _currentUser.Id.Returns("owner-1");
        _sut = new GetPetHandler(_pets, _currentUser);
    }

    [Fact]
    public async Task HandleAsync_OwnPet_ReturnsItWithMedicalNotes()
    {
        Pet luna = PetMother.Luna();
        _pets.GetAsync(luna.Id, Arg.Any<CancellationToken>()).Returns(luna);

        WaggoResponse<PetResponse> result = await _sut.HandleAsync(new GetPetQuery(luna.Id), CancellationToken.None);

        result.Data.ShouldBe(PetResponse.From(luna));
        result.Data.MedicalNotes.ShouldBe("Alérgica al pollo");
    }

    [Fact]
    public async Task HandleAsync_PetOfAnotherOwner_ThrowsNotFound()
    {
        Pet foreign = PetMother.Luna(ownerId: "owner-2");
        _pets.GetAsync(foreign.Id, Arg.Any<CancellationToken>()).Returns(foreign);

        NotFoundException exception = await Should.ThrowAsync<NotFoundException>(
            () => _sut.HandleAsync(new GetPetQuery(foreign.Id), CancellationToken.None));

        exception.Error.ShouldBe(PetErrors.NotFound);
    }

    [Fact]
    public async Task HandleAsync_UnknownPet_ThrowsNotFound()
    {
        await Should.ThrowAsync<NotFoundException>(
            () => _sut.HandleAsync(new GetPetQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
