using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.GoogleRoutesApi.Models
{
    public class Route
    {
        [JsonProperty("routeLabels")]
        public List<string> RouteLabels { get; set; } = new List<string>();

        [JsonProperty("legs")]
        public List<Leg> Legs { get; set; } = new List<Leg>();

        [JsonProperty("distanceMeters")]
        public int DistanceMeters { get; set; }

        [JsonProperty("duration")]
        public string Duration { get; set; }

        [JsonProperty("polyline")]
        public Polyline Polyline { get; set; }

        [JsonProperty("warnings")]
        public List<string> Warnings { get; set; } = new List<string>();

        [JsonProperty("travelAdvisory")]
        public TravelAdvisory TravelAdvisory { get; set; }

        [JsonProperty("routeToken")]
        public string RouteToken { get; set; }
    }
}
