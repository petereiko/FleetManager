using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ApiModels.GoogleRoutes
{
    public class RouteRequest
    {
        [Required(ErrorMessage = "Origin address is required")]
        public string OriginAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Destination address is required")]
        public string DestinationAddress { get; set; } = string.Empty;

        public List<string>? IntermediateAddresses { get; set; }

        public string TravelMode { get; set; } = "DRIVE"; // DRIVE, BICYCLE, WALK, TWO_WHEELER

        public string RoutingPreference { get; set; } = "TRAFFIC_AWARE"; // TRAFFIC_UNAWARE, TRAFFIC_AWARE, TRAFFIC_AWARE_OPTIMAL

        public bool ComputeAlternativeRoutes { get; set; } = true;

        public bool AvoidTolls { get; set; } = false;

        public bool AvoidHighways { get; set; } = false;

        public bool AvoidFerries { get; set; } = false;

        public string Units { get; set; } = "METRIC"; // METRIC, IMPERIAL
    }
}
