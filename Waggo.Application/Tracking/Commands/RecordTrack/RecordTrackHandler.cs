using FluentValidation;
using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Tracking;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Tracking;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Enums.Walks;
using Waggo.Domain.Errors.Tracking;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.Exceptions;
using Waggo.Domain.ValueObjects.Walks;

namespace Waggo.Application.Tracking.Commands.RecordTrack;

/// <summary>RF-008: the assigned walker's phone sends the positions of the walk in progress, in batches.</summary>
internal sealed class RecordTrackHandler(
    IValidator<RecordTrackCommand> validator,
    IWalkRepository walks,
    ITrackPointRepository points,
    ICurrentUser currentUser)
    : ICommandHandler<RecordTrackCommand, int>
{
    public async Task<WaggoResponse<int>> HandleAsync(RecordTrackCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<int> response = new();

        response.ConcatStacks(await validator.ValidateToResponseAsync(command, cancellationToken));
        if (!response.IsValid)
        {
            return response;
        }

        Walk? walk = await walks.GetAsync(command.WalkId, cancellationToken);
        if (walk is null || walk.WalkerId != currentUser.Id)
        {
            throw new NotFoundException(
                WalkErrors.NotFound,
                $"Walk {command.WalkId} is not assigned to {currentUser.Id}");
        }

        if (walk.Status != WalkStatus.InProgress)
        {
            response.AddError(TrackingErrors.WalkNotInProgress);
            return response;
        }

        List<TrackPoint> batch =
        [
            .. command.Points.Select(point => TrackPoint.Record(
                walk.Id,
                GeoPoint.Create(point.Latitude, point.Longitude).Data,
                point.RecordedAt)),
        ];
        await points.AddRangeAsync(batch, cancellationToken);
        response.Data = batch.Count;
        return response;
    }
}
