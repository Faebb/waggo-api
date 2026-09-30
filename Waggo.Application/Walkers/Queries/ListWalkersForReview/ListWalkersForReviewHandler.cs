using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walkers;

namespace Waggo.Application.Walkers.Queries.ListWalkersForReview;

/// <summary>RF-003: the admin sees the walkers waiting for verification.</summary>
internal sealed class ListWalkersForReviewHandler(IWalkerProfileRepository profiles)
    : IQueryHandler<ListWalkersForReviewQuery, IReadOnlyList<WalkerProfileResponse>>
{
    public async Task<WaggoResponse<IReadOnlyList<WalkerProfileResponse>>> HandleAsync(
        ListWalkersForReviewQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        IReadOnlyList<WalkerProfile> found = await profiles.ListByStatusAsync(query.Status, cancellationToken);
        return new WaggoResponse<IReadOnlyList<WalkerProfileResponse>>
        {
            Data = [.. found.Select(WalkerProfileResponse.From)],
        };
    }
}
