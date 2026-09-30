using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Application.UnitTests.TestDoubles;
using Waggo.Domain.Entities.Walkers;
using Waggo.Domain.Enums.Walkers;

namespace Waggo.Application.UnitTests.TestData;

/// <summary>Object Mother for walker profiles (RF-002, RF-003).</summary>
internal static class WalkerMother
{
    public static WalkerProfile Pending(string userId = "walker-1") =>
        WalkerProfile.Register(
            userId,
            "Andrés Gómez",
            DocumentType.CC,
            "1020304050",
            "3001234567",
            "3 años con perros grandes",
            FixedTimeProvider.Default).Data;

    public static WalkerProfile Approved(string userId = "walker-1")
    {
        WalkerProfile profile = Pending(userId);
        profile.Approve("admin-1", FixedTimeProvider.Default);
        return profile;
    }

    /// <summary>A repository where <paramref name="userId"/> is a verified walker.</summary>
    public static IWalkerProfileRepository VerifiedRepository(string userId)
    {
        IWalkerProfileRepository profiles = Substitute.For<IWalkerProfileRepository>();
        profiles.GetByUserAsync(userId, Arg.Any<CancellationToken>()).Returns(Approved(userId));
        return profiles;
    }
}
