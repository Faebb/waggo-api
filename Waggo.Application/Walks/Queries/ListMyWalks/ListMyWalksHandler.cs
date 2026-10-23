using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Walkers;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walks;

namespace Waggo.Application.Walks.Queries.ListMyWalks;

/// <summary>RF-007: the owner sees their walks, never another owner's.</summary>
internal sealed class ListMyWalksHandler(
    IWalkRepository walks,
    IWalkerProfileRepository walkerProfiles,
    ICurrentUser currentUser)
    : IQueryHandler<ListMyWalksQuery, IReadOnlyList<WalkResponse>>
{
    public async Task<WaggoResponse<IReadOnlyList<WalkResponse>>> HandleAsync(
        ListMyWalksQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Walk> mine = await walks.ListByOwnerAsync(currentUser.Id, cancellationToken);
        return new WaggoResponse<IReadOnlyList<WalkResponse>>
        {
            Data = await walkerProfiles.ToWalkResponsesAsync(mine, cancellationToken),
        };
    }
}
