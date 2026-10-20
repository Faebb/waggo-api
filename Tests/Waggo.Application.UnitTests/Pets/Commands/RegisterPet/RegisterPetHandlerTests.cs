using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Pets;
using Waggo.Application.Pets;
using Waggo.Application.Pets.Commands.RegisterPet;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Pets;
using Waggo.Domain.Enums.Common;
using Waggo.Domain.Enums.Pets;
using Waggo.Domain.Errors.Pets;

namespace Waggo.Application.UnitTests.Pets.Commands.RegisterPet;

public class RegisterPetHandlerTests
{
    private readonly IPetRepository _pets = Substitute.For<IPetRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly RegisterPetHandler _sut;

    public RegisterPetHandlerTests()
    {
        _currentUser.Id.Returns("owner-1");
        _sut = new RegisterPetHandler(new RegisterPetCommandValidator(), _pets, _currentUser, new FixedTimeProvider());
    }

    private static RegisterPetCommand Luna(decimal? weightKg = 14.5m, string name = "Luna") =>
        new(name, "Criolla", PetSize.Medium, new DateOnly(2021, 5, 10), weightKg, "Alérgica al pollo");

    [Fact]
    public async Task HandleAsync_ValidCommand_SavesThePetForTheCurrentOwner()
    {
        WaggoResponse<PetResponse> result = await _sut.HandleAsync(Luna(), CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.Data.Name.ShouldBe("Luna");
        result.Data.Size.ShouldBe("Medium");
        result.Data.MedicalNotes.ShouldBe("Alérgica al pollo");
        await _pets.Received(1).AddAsync(
            Arg.Is<Pet>(pet => pet.OwnerId == "owner-1" && pet.Id == result.Data.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InvalidWeight_FailsWithoutSaving()
    {
        WaggoResponse<PetResponse> result = await _sut.HandleAsync(Luna(weightKg: 120m), CancellationToken.None);

        result.Errors.ShouldContain(e => e.Code == PetErrors.InvalidWeight.Code);
        result.ErrorType.ShouldBe(ErrorType.Validation);
        await _pets.DidNotReceive().AddAsync(Arg.Any<Pet>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NameTooLong_FailsWithInvalidName()
    {
        WaggoResponse<PetResponse> result =
            await _sut.HandleAsync(Luna(name: new string('a', 51)), CancellationToken.None);

        result.Errors.ShouldContain(e => e.Code == PetErrors.InvalidName.Code);
    }

    [Fact]
    public async Task HandleAsync_OwnerAlreadyHasTheMaximum_FailsWithLimitReached()
    {
        _pets.CountByOwnerAsync("owner-1", Arg.Any<CancellationToken>()).Returns(Pet.MaxPetsPerOwner);

        WaggoResponse<PetResponse> result = await _sut.HandleAsync(Luna(), CancellationToken.None);

        result.Errors.Single().Code.ShouldBe(PetErrors.LimitReached.Code);
        result.ErrorType.ShouldBe(ErrorType.BusinessRule);
        await _pets.DidNotReceive().AddAsync(Arg.Any<Pet>(), Arg.Any<CancellationToken>());
    }
}
