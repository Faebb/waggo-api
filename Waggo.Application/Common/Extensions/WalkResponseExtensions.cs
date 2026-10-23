using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Application.Walks;
using Waggo.Domain.Entities.Walkers;
using Waggo.Domain.Entities.Walks;

namespace Waggo.Application.Common.Extensions;

/// <summary>
/// Builds <see cref="WalkResponse"/> with the assigned walker's name (RF-007, RF-008). Call it only after checking
/// that the current user is the owner or the walker of the walk: the name is never shown to anybody else.
/// </summary>
internal static class WalkResponseExtensions
{
    public static async Task<WalkResponse> ToWalkResponseAsync(
        this IWalkerProfileRepository walkerProfiles,
        Walk walk,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(walk);
        WalkerProfile? profile = walk.WalkerId is null
            ? null
            : await walkerProfiles.GetByUserAsync(walk.WalkerId, cancellationToken);
        return WalkResponse.From(walk, profile);
    }

    /// <summary>Same as <see cref="ToWalkResponseAsync"/> for a list, with one query for all the walkers.</summary>
    public static async Task<IReadOnlyList<WalkResponse>> ToWalkResponsesAsync(
        this IWalkerProfileRepository walkerProfiles,
        IReadOnlyList<Walk> walks,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(walks);
        string[] walkerIds = [.. walks.Select(walk => walk.WalkerId).OfType<string>().Distinct()];
        Dictionary<string, WalkerProfile> profiles = walkerIds.Length == 0
            ? []
            : (await walkerProfiles.ListByUsersAsync(walkerIds, cancellationToken))
                .ToDictionary(profile => profile.UserId);

        return
        [
            .. walks.Select(walk => WalkResponse.From(
                walk,
                walk.WalkerId is null ? null : profiles.GetValueOrDefault(walk.WalkerId))),
        ];
    }
}
