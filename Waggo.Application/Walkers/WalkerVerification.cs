using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Domain.Entities.Walkers;
using Waggo.Domain.Errors.Walkers;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.Walkers;

/// <summary>RF-003 gate: only verified walkers see requests and accept walks.</summary>
internal static class WalkerVerification
{
    public static async Task EnsureVerifiedAsync(
        this IWalkerProfileRepository profiles,
        string userId,
        CancellationToken cancellationToken)
    {
        WalkerProfile? profile = await profiles.GetByUserAsync(userId, cancellationToken);
        if (profile is not { IsVerified: true })
        {
            throw new ForbiddenException(
                WalkerErrors.NotVerified,
                $"Walker {userId} is {profile?.Status.ToString() ?? "not registered"}");
        }
    }
}
