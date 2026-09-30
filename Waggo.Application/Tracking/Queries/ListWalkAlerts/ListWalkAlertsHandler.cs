using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Tracking;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Tracking;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.Tracking.Queries.ListWalkAlerts;

/// <summary>RF-012: the owner and the assigned walker see the alerts of the walk.</summary>
internal sealed class ListWalkAlertsHandler(
    IWalkRepository walks,
    IWalkAlertRepository alerts,
    ICurrentUser currentUser)
    : IQueryHandler<ListWalkAlertsQuery, IReadOnlyList<WalkAlertResponse>>
{
    public async Task<WaggoResponse<IReadOnlyList<WalkAlertResponse>>> HandleAsync(
        ListWalkAlertsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        Walk? walk = await walks.GetAsync(query.WalkId, cancellationToken);
        if (walk is null || (walk.OwnerId != currentUser.Id && walk.WalkerId != currentUser.Id))
        {
            throw new NotFoundException(WalkErrors.NotFound, $"Walk {query.WalkId} not found for {currentUser.Id}");
        }

        IReadOnlyList<WalkAlert> stored = await alerts.ListByWalkAsync(walk.Id, cancellationToken);
        return new WaggoResponse<IReadOnlyList<WalkAlertResponse>>
        {
            Data = [.. stored.OrderByDescending(alert => alert.RaisedAt).Select(WalkAlertResponse.From)],
        };
    }
}
