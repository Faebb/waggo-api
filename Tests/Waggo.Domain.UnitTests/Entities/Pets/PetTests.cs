using Waggo.Domain.Common;
using Waggo.Domain.Entities.Pets;
using Waggo.Domain.Enums.Pets;
using Waggo.Domain.Errors.Pets;

namespace Waggo.Domain.UnitTests.Entities.Pets;

public class PetTests
{
    private static readonly DateTimeOffset s_now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    private static WaggoResponse<Pet> Register(
        string name = "Luna",
        string? breed = "Criolla",
        DateOnly? birthDate = null,
        decimal? weightKg = 14.5m,
        string? medicalNotes = "Alérgica al pollo") =>
        Pet.Register("owner-1", name, breed, PetSize.Medium, birthDate, weightKg, medicalNotes, s_now);

    [Fact]
    public void Register_ValidData_CreatesThePet()
    {
        WaggoResponse<Pet> result = Register(birthDate: new DateOnly(2021, 5, 10));

        result.IsValid.ShouldBeTrue();
        Pet pet = result.Data;
        pet.Id.ShouldNotBe(Guid.Empty);
        pet.OwnerId.ShouldBe("owner-1");
        pet.Name.ShouldBe("Luna");
        pet.Breed.ShouldBe("Criolla");
        pet.Size.ShouldBe(PetSize.Medium);
        pet.BirthDate.ShouldBe(new DateOnly(2021, 5, 10));
        pet.WeightKg.ShouldBe(14.5m);
        pet.MedicalNotes.ShouldBe("Alérgica al pollo");
        pet.RegisteredAt.ShouldBe(s_now);
    }

    [Fact]
    public void Register_TrimsTextAndTurnsBlankOptionalsIntoNull()
    {
        Pet pet = Register(name: "  Max ", breed: "   ", medicalNotes: " ").Data;

        pet.Name.ShouldBe("Max");
        pet.Breed.ShouldBeNull();
        pet.MedicalNotes.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Un nombre larguísimo que supera los cincuenta caracteres")]
    public void Register_InvalidName_FailsWithInvalidName(string name) =>
        Register(name: name).Errors.ShouldContain(e => e.Code == PetErrors.InvalidName.Code);

    [Fact]
    public void Register_BreedLongerThan50_FailsWithInvalidBreed() =>
        Register(breed: new string('a', 51)).Errors.ShouldContain(e => e.Code == PetErrors.InvalidBreed.Code);

    [Theory]
    [InlineData(0.2)]
    [InlineData(120)]
    public void Register_WeightOutOfRange_FailsWithInvalidWeight(double weightKg) =>
        Register(weightKg: (decimal)weightKg).Errors.ShouldContain(e => e.Code == PetErrors.InvalidWeight.Code);

    [Theory]
    [InlineData(0.5)]
    [InlineData(100)]
    public void Register_WeightOnTheLimits_Succeeds(double weightKg) =>
        Register(weightKg: (decimal)weightKg).IsValid.ShouldBeTrue();

    [Fact]
    public void Register_BirthDateInTheFuture_FailsWithInvalidBirthDate() =>
        Register(birthDate: new DateOnly(2026, 10, 1))
            .Errors.ShouldContain(e => e.Code == PetErrors.InvalidBirthDate.Code);

    [Fact]
    public void Register_BirthDateOlderThan30Years_FailsWithInvalidBirthDate() =>
        Register(birthDate: new DateOnly(1995, 1, 1))
            .Errors.ShouldContain(e => e.Code == PetErrors.InvalidBirthDate.Code);

    [Fact]
    public void Register_MedicalNotesLongerThan2000_FailsWithInvalidMedicalNotes() =>
        Register(medicalNotes: new string('a', 2001))
            .Errors.ShouldContain(e => e.Code == PetErrors.InvalidMedicalNotes.Code);

    [Fact]
    public void Register_SeveralInvalidFields_ReportsEveryError()
    {
        WaggoResponse<Pet> result = Register(name: "", weightKg: 0m);

        result.Errors.Select(e => e.Code).ShouldBe([PetErrors.InvalidName.Code, PetErrors.InvalidWeight.Code]);
    }
}
