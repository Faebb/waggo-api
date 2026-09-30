using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Tracking.Commands.RecordTrack;

/// <summary>A batch of positions of a walk in progress. Returns how many were saved.</summary>
public sealed record RecordTrackCommand(Guid WalkId, IReadOnlyList<TrackPointInput> Points) : ICommand<int>;
