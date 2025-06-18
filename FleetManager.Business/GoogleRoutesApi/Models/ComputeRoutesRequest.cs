using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.GoogleRoutesApi.Models
{
    public class ComputeRoutesRequest
    {
        [JsonProperty("origin")]
        public Waypoint Origin { get; set; }

        [JsonProperty("destination")]
        public Waypoint Destination { get; set; }

        [JsonProperty("intermediates")]
        public List<Waypoint> Intermediates { get; set; } = new List<Waypoint>();

        [JsonProperty("travelMode")]
        public string TravelMode { get; set; } = "DRIVE"; // DRIVE, BICYCLE, WALK, TWO_WHEELER

        [JsonProperty("routingPreference")]
        public string RoutingPreference { get; set; } = "TRAFFIC_AWARE"; // TRAFFIC_UNAWARE, TRAFFIC_AWARE, TRAFFIC_AWARE_OPTIMAL

        [JsonProperty("computeAlternativeRoutes")]
        public bool ComputeAlternativeRoutes { get; set; } = false;

        [JsonProperty("routeModifiers")]
        public RouteModifiers RouteModifiers { get; set; } = new RouteModifiers();


        [JsonProperty("languageCode")]
        public string LanguageCode { get; set; } = "en-US";

        [JsonProperty("units")]
        public string Units { get; set; } = "METRIC"; // METRIC, IMPERIAL
    }
}
