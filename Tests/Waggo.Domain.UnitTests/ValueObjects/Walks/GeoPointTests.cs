using Waggo.Domain.Errors.Walks;
using Waggo.Domain.ValueObjects.Walks;

namespace Waggo.Domain.UnitTests.ValueObjects.Walks;

public class GeoPointTests
{
    [Theory]
    [InlineData(4.6361, -74.0645)]
    [InlineData(-90, 180)]
    [InlineData(90, -180)]
    public void Create_ValidCoordinates_Succeeds(double latitude, double longitude)
    {
        GeoPoint point = GeoPoint.Create(latitude, longitude).Data;

        point.Latitude.ShouldBe(latitude);
        point.Longitude.ShouldBe(longitude);
    }

    [Fact]
    public void DistanceKmTo_TwoPointsInBogota_IsTheStraightLineDistance()
    {
        GeoPoint parqueNacional = GeoPoint.Create(4.6250, -74.0650).Data;
        GeoPoint parque93 = GeoPoint.Create(4.6765, -74.0480).Data;

        parqueNacional.DistanceKmTo(parque93).ShouldBe(6.0, 0.2);
    }

    [Fact]
    public void DistanceKmTo_SamePoint_IsZero()
    {
        GeoPoint point = GeoPoint.Create(4.6361, -74.0645).Data;

        point.DistanceKmTo(point).ShouldBe(0, 0.000001);
    }

    [Theory]
    [InlineData(95, -74)]
    [InlineData(-90.1, 0)]
    [InlineData(4.6, 181)]
    [InlineData(double.NaN, 0)]
    public void Create_InvalidCoordinates_FailsWithInvalidLocation(double latitude, double longitude) =>
        GeoPoint.Create(latitude, longitude).Errors.ShouldContain(e => e.Code == WalkErrors.InvalidLocation.Code);
}
