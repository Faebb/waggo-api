using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walks;

namespace Waggo.Application.Walks.Queries.ListAssignedWalks;

/// <summary>RF-007: the walker sees the walks they accepted.</summary>
internal sealed class ListAssignedWalksHandler(IWalkRepository walks, ICurrentUser currentUser)
    : IQueryHandler<ListAssignedWalksQuery, IReadOnlyList<WalkResponse>>
{
    public async Task<WaggoResponse<IReadOnlyList<WalkResponse>>> HandleAsync(
        ListAssignedWalksQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Walk> assigned = await walks.ListByWalkerAsync(currentUser.Id, cancellationToken);
        return new WaggoResponse<IReadOnlyList<WalkResponse>> { Data = [.. assigned.Select(WalkResponse.From)] };
    }
}
