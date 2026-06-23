using StudentPortal.Api.Services.Interfaces;

namespace StudentPortal.Api.Services.Implementations;

public class GeofenceService : IGeofenceService
{
    private const double EarthRadiusMeters = 6371000.0;

    public bool IsWithinRadius(double studentLat, double studentLng,
                                double sessionLat, double sessionLng,
                                int radiusMeters)
    {
        // Convert coordinates to radians
        double dLat = ToRadians(sessionLat - studentLat);
        double dLng = ToRadians(sessionLng - studentLng);

        double rStudentLat = ToRadians(studentLat);
        double rSessionLat = ToRadians(sessionLat);

        // Haversine formula
        double a = Math.Pow(Math.Sin(dLat / 2.0), 2.0) +
                   Math.Cos(rStudentLat) * Math.Cos(rSessionLat) *
                   Math.Pow(Math.Sin(dLng / 2.0), 2.0);

        double c = 2.0 * Math.Asin(Math.Min(1.0, Math.Sqrt(a)));
        double distance = EarthRadiusMeters * c;

        return distance <= radiusMeters;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180.0);
    }
}
