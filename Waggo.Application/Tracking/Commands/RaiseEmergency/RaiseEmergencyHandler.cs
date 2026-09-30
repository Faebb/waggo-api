using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Tracking;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Tracking;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Enums.Tracking;
using Waggo.Domain.Enums.Walks;
using Waggo.Domain.Errors.Tracking;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.Exceptions;
using Waggo.Domain.ValueObjects.Walks;

namespace Waggo.Application.Tracking.Commands.RaiseEmergency;

/// <summary>
/// RF-012: the owner or the assigned walker raises an emergency while the walk is accepted or going on.
/// </summary>
internal sealed class RaiseEmergencyHandler(
    IWalkRepository walks,
    IWalkAlertRepository alerts,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<RaiseEmergencyCommand, WalkAlertResponse>
{
    public async Task<WaggoResponse<WalkAlertResponse>> HandleAsync(
        RaiseEmergencyCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<WalkAlertResponse> response = new();

        Walk? walk = await walks.GetAsync(command.WalkId, cancellationToken);
        AlertParty? party = walk is null ? null
            : walk.OwnerId == currentUser.Id ? AlertParty.Owner
            : walk.WalkerId == currentUser.Id ? AlertParty.Walker
            : null;
        if (walk is null || party is null)
        {
            throw new NotFoundException(WalkErrors.NotFound, $"Walk {command.WalkId} not found for {currentUser.Id}");
        }

        if (walk.Status is not (WalkStatus.Accepted or WalkStatus.InProgress))
        {
            response.AddError(AlertErrors.WalkNotActive);
            return response;
        }

        GeoPoint? location = null;
        if (command.Latitude is not null || command.Longitude is not null)
        {
            // Only one coordinate is not a position: NaN makes GeoPoint reject it.
            WaggoResponse<GeoPoint> point =
                GeoPoint.Create(command.Latitude ?? double.NaN, command.Longitude ?? double.NaN);
            response.ConcatStacks(point);
            if (!response.IsValid)
            {
                return response;
            }

            location = point.Data;
        }

        WaggoResponse<WalkAlert> alert = WalkAlert.RaiseEmergency(
            walk.Id,
            party.Value,
            command.Message,
            location,
            timeProvider.GetUtcNow());
        response.ConcatStacks(alert);
        if (!response.IsValid)
        {
            return response;
        }

        await alerts.AddAsync(alert.Data, cancellationToken);
        response.Data = WalkAlertResponse.From(alert.Data);
        return response;
    }
}
