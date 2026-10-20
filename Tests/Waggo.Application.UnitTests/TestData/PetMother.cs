using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Entities.Pets;
using Waggo.Domain.Enums.Pets;

namespace Waggo.Application.UnitTests.TestData;

/// <summary>Object Mother with the dogs used in the RF-004 spec examples.</summary>
internal static class PetMother
{
    public static Pet Luna(string ownerId = "owner-1") =>
        Pet.Register(
            ownerId,
            "Luna",
            "Criolla",
            PetSize.Medium,
            new DateOnly(2021, 5, 10),
            14.5m,
            "Alérgica al pollo",
            FixedTimeProvider.Default).Data;

    public static Pet Max(string ownerId = "owner-1") =>
        Pet.Register(ownerId, "Max", null, PetSize.Small, null, null, null, FixedTimeProvider.Default).Data;
}
