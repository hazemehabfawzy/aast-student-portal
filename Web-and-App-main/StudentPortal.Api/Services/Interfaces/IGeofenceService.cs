namespace StudentPortal.Api.Services.Interfaces;

public interface IGeofenceService
{
    bool IsWithinRadius(double studentLat, double studentLng,
                        double sessionLat, double sessionLng,
                        int radiusMeters);
}
