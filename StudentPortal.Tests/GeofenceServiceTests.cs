using StudentPortal.Api.Services.Implementations;
using Xunit;

namespace StudentPortal.Tests;

public class GeofenceServiceTests
{
    private readonly GeofenceService _service = new();

    [Fact]
    public void IsWithinRadius_SamePoint_ReturnsTrue()
    {
        // Arrange
        double lat = 30.0818;
        double lng = 31.3235;
        int radius = 10;

        // Act
        bool result = _service.IsWithinRadius(lat, lng, lat, lng, radius);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsWithinRadius_Points100mApart_Radius50_ReturnsFalse()
    {
        // Arrange
        double latA = 30.0818;
        double lngA = 31.3235;
        double latB = 30.0818;
        double lngB = 31.32454; // ~100m away
        int radius = 50;

        // Act
        bool result = _service.IsWithinRadius(latA, lngA, latB, lngB, radius);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsWithinRadius_Points100mApart_Radius150_ReturnsTrue()
    {
        // Arrange
        double latA = 30.0818;
        double lngA = 31.3235;
        double latB = 30.0818;
        double lngB = 31.32454; // ~100m away
        int radius = 150;

        // Act
        bool result = _service.IsWithinRadius(latA, lngA, latB, lngB, radius);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsWithinRadius_AASTCairoCampusCoordinates_RealisticTest()
    {
        // Arrange
        // Coordinates for AAST Cairo (Heliopolis Building A vs Building B/Court)
        double buildingA_Lat = 30.08180;
        double buildingA_Lng = 31.32350;
        double court_Lat = 30.08210; // ~33m North
        double court_Lng = 31.32360; // ~10m East
        // Distance should be ~35 meters
        
        // Act & Assert
        // Within 50m radius should be true
        Assert.True(_service.IsWithinRadius(buildingA_Lat, buildingA_Lng, court_Lat, court_Lng, 50));
        // Within 20m radius should be false
        Assert.False(_service.IsWithinRadius(buildingA_Lat, buildingA_Lng, court_Lat, court_Lng, 20));
    }
}
