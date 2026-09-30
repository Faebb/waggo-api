using Waggo.Domain.Common;
using Waggo.Domain.Errors.Walks;

namespace Waggo.Domain.ValueObjects.Walks;

/// <summary>A position on Earth in WGS 84 degrees (the GPS standard, SRID 4326).</summary>
public sealed record GeoPoint
{
    private GeoPoint(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }

    public double Longitude { get; }

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
}
