using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;
using Waggo.Domain.ValueObjects.Walks;

namespace Waggo.Infrastructure.Persistence.Converters;

/// <summary>
/// Stores a <see cref="GeoPoint"/> as a PostGIS <c>geography(Point, 4326)</c>. The domain stays free of GIS
/// libraries; NetTopologySuite only lives here. Note the order: a Point is (x = longitude, y = latitude).
/// </summary>
internal sealed class GeoPointConverter()
    : ValueConverter<GeoPoint, Point>(
        point => new Point(point.Longitude, point.Latitude) { SRID = 4326 },
        point => GeoPoint.Create(point.Y, point.X).Data);
