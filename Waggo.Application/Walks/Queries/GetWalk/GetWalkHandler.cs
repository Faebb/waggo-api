using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.Walks.Queries.GetWalk;

/// <summary>RF-007: detail of one of the owner's walks (the owner polls it to see when a walker accepts).</summary>
internal sealed class GetWalkHandler(IWalkRepository walks, ICurrentUser currentUser)
    : IQueryHandler<GetWalkQuery, WalkResponse>
{
    public async Task<WaggoResponse<WalkResponse>> HandleAsync(GetWalkQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        Walk? walk = await walks.GetAsync(query.Id, cancellationToken);

        // Another owner's walk answers the same as a missing one, so ids of other owners are not revealed.
        if (walk is null || walk.OwnerId != currentUser.Id)
        {
            throw new NotFoundException(WalkErrors.NotFound, $"Walk {query.Id} not found for {currentUser.Id}");
        }

        return new WaggoResponse<WalkResponse> { Data = WalkResponse.From(walk) };
    }
}
