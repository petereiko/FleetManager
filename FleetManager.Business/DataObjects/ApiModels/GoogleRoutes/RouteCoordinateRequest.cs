using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ApiModels.GoogleRoutes
{
    public class RouteCoordinateRequest
    {
        [Required]
        [Range(-90, 90)]
        public double OriginLatitude { get; set; }

        [Required]
        [Range(-180, 180)]
        public double OriginLongitude { get; set; }

        [Required]
        [Range(-90, 90)]
        public double DestinationLatitude { get; set; }

        [Required]
        [Range(-180, 180)]
        public double DestinationLongitude { get; set; }

        public List<CoordinatePair>? IntermediatePoints { get; set; }

        public string TravelMode { get; set; } = "DRIVE";

        public string RoutingPreference { get; set; } = "TRAFFIC_AWARE";

        public bool ComputeAlternativeRoutes { get; set; } = true;

        public bool AvoidTolls { get; set; } = false;

        public bool AvoidHighways { get; set; } = false;

        public bool AvoidFerries { get; set; } = false;
    }
}
