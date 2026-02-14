using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ApiModels.GoogleRoutes
{
    public class RouteOption
    {
        public List<string> RouteLabels { get; set; } = new List<string>();
        public int DistanceMeters { get; set; }
        public string DistanceText { get; set; } = string.Empty; // "45.5 km"
        public string Duration { get; set; } = string.Empty; // "2345s"
        public int DurationMinutes { get; set; } // Calculated for mobile app
        public string DurationText { get; set; } = string.Empty; // "39 mins"
        public string EncodedPolyline { get; set; } = string.Empty;
        public List<RouteLeg> Legs { get; set; } = new List<RouteLeg>();
        public List<string> Warnings { get; set; } = new List<string>();
        public TollInformation? TollInfo { get; set; }
        public bool HasTolls { get; set; }
        public string RouteToken { get; set; } = string.Empty;
    }
}
