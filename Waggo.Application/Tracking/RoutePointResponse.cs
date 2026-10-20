namespace Waggo.Application.Tracking;

public sealed record RoutePointResponse(double Latitude, double Longitude, DateTimeOffset RecordedAt);
