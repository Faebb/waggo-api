using Waggo.Application.Tracking.Commands.RecordTrack;

namespace Waggo.Api.Endpoints.Tracking.Requests;

/// <summary>Body of <c>POST /api/v1/walks/{id}/track</c>: a batch of positions. Its size is a business rule.</summary>
public sealed record RecordTrackRequest(List<TrackPointRequest>? Points)
{
    public RecordTrackCommand ToCommand(Guid walkId) =>
        new(walkId, [.. (Points ?? []).Select(ToInput)]);

    private static TrackPointInput ToInput(TrackPointRequest point) =>
        new(point.Latitude, point.Longitude, point.RecordedAt);
}
