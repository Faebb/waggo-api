using Waggo.Domain.Common;
using Waggo.Domain.Errors.Walks;

namespace Waggo.Domain.ValueObjects.Walks;

/// <summary>A position on Earth in WGS 84 degrees (the GPS standard, SRID 4326).</summary>
public sealed record GeoPoint
{
    private const double EarthRadiusKm = 6371.0;

    private GeoPoint(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }

    public double Longitude { get; }

    /// <summary>Straight-line distance over the Earth's surface (haversine), in kilometers.</summary>
    public double DistanceKmTo(GeoPoint other)
    {
        ArgumentNullException.ThrowIfNull(other);

        double dLatitude = ToRadians(other.Latitude - Latitude);
        double dLongitude = ToRadians(other.Longitude - Longitude);
        double a = (Math.Sin(dLatitude / 2) * Math.Sin(dLatitude / 2))
            + (Math.Cos(ToRadians(Latitude)) * Math.Cos(ToRadians(other.Latitude))
                * Math.Sin(dLongitude / 2) * Math.Sin(dLongitude / 2));
        return EarthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    public static WaggoResponse<GeoPoint> Create(double latitude, double longitude)
    {
        WaggoResponse<GeoPoint> response = new();

        if (latitude is not (>= -90 and <= 90) || longitude is not (>= -180 and <= 180))
        {
            response.AddError(WalkErrors.InvalidLocation);
            return response;
        }

        response.Data = new GeoPoint(latitude, longitude);
        return response;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
