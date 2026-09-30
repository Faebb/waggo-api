namespace Waggo.Api.Endpoints.Tracking.Requests;

public sealed record TrackPointRequest(double Latitude, double Longitude, DateTimeOffset RecordedAt);
