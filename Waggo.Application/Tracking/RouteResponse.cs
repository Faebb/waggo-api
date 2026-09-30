namespace Waggo.Application.Tracking;

/// <summary>Route of a walk with its summary (RF-008, RF-011).</summary>
public sealed record RouteResponse(IReadOnlyList<RoutePointResponse> Points, double DistanceKm, int ElapsedMinutes);
