namespace Waggo.Application.Tracking.Commands.RecordTrack;

/// <summary>One position sent by the walker's phone.</summary>
public sealed record TrackPointInput(double Latitude, double Longitude, DateTimeOffset RecordedAt);
