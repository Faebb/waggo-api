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

    [Theory]
    [InlineData(95, -74)]
    [InlineData(-90.1, 0)]
    [InlineData(4.6, 181)]
    [InlineData(double.NaN, 0)]
    public void Create_InvalidCoordinates_FailsWithInvalidLocation(double latitude, double longitude) =>
        GeoPoint.Create(latitude, longitude).Errors.ShouldContain(e => e.Code == WalkErrors.InvalidLocation.Code);
}
