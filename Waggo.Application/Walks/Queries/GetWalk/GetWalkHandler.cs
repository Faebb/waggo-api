using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.Walks.Queries.GetWalk;

/// <summary>
/// RF-007: detail of a walk for its owner (who polls it to see when a walker accepts) or for the walker who took it.
/// </summary>
internal sealed class GetWalkHandler(IWalkRepository walks, ICurrentUser currentUser)
    : IQueryHandler<GetWalkQuery, WalkResponse>
{
    public async Task<WaggoResponse<WalkResponse>> HandleAsync(GetWalkQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        Walk? walk = await walks.GetAsync(query.Id, cancellationToken);

        // Anybody else gets the same answer as for a missing walk, so ids of other users are not revealed.
        if (walk is null || (walk.OwnerId != currentUser.Id && walk.WalkerId != currentUser.Id))
        {
            throw new NotFoundException(WalkErrors.NotFound, $"Walk {query.Id} not found for {currentUser.Id}");
        }

        return new WaggoResponse<WalkResponse> { Data = WalkResponse.From(walk) };
    }
}
