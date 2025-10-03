using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.UtilityModels
{
    public static class GeoUtils
    {
        private const double EarthRadiusMeters = 6371000.0;

        private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

        // Primary: double-based implementation
        public static double HaversineDistanceMeters(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Pow(Math.Sin(dLon / 2), 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusMeters * c;
        }

        // Convenience overloads for decimal inputs (for minimal changes to your models/DTOs)
        public static double HaversineDistanceMeters(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
            => HaversineDistanceMeters((double)lat1, (double)lon1, (double)lat2, (double)lon2);

        public static double? HaversineDistanceMeters(decimal? lat1, decimal? lon1, decimal? lat2, decimal? lon2)
        {
            if (!lat1.HasValue || !lon1.HasValue || !lat2.HasValue || !lon2.HasValue) return null;
            return HaversineDistanceMeters(lat1.Value, lon1.Value, lat2.Value, lon2.Value);
        }

        public static double? HaversineDistanceMeters(double? lat1, double? lon1, double? lat2, double? lon2)
        {
            if (!lat1.HasValue || !lon1.HasValue || !lat2.HasValue || !lon2.HasValue) return null;
            return HaversineDistanceMeters(lat1.Value, lon1.Value, lat2.Value, lon2.Value);
        }
    }
}
