using FluentValidation;
using Waggo.Application.Common.Extensions;
using Waggo.Domain.Errors.Tracking;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.Services.Tracking;

namespace Waggo.Application.Tracking.Commands.RecordTrack;

/// <summary>Batch size (RNF-007) and valid coordinates of every position.</summary>
internal sealed class RecordTrackCommandValidator : AbstractValidator<RecordTrackCommand>
{
    public RecordTrackCommandValidator()
    {
        RuleFor(command => command.Points)
            .Cascade(CascadeMode.Stop)
            .Must(points => points is { Count: > 0 and <= RouteMetrics.MaxPointsPerBatch })
            .WithError(TrackingErrors.InvalidBatch)
            .Must(points => points.All(p => p.Latitude is >= -90 and <= 90 && p.Longitude is >= -180 and <= 180))
            .WithError(WalkErrors.InvalidLocation);
    }
}
