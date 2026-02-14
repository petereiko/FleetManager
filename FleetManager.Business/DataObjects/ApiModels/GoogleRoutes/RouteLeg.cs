using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ApiModels.GoogleRoutes
{
    public class RouteLeg
    {
        public int DistanceMeters { get; set; }
        public string Duration { get; set; } = string.Empty;
        public RouteLocation StartLocation { get; set; } = new RouteLocation();
        public RouteLocation EndLocation { get; set; } = new RouteLocation();
        public List<RouteStep> Steps { get; set; } = new List<RouteStep>();
    }
}
