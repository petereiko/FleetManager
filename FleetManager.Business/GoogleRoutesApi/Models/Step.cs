using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.GoogleRoutesApi.Models
{
    public class Step
    {
        [JsonProperty("distanceMeters")]
        public int DistanceMeters { get; set; }

        [JsonProperty("duration")]
        public string Duration { get; set; }

        [JsonProperty("polyline")]
        public Polyline Polyline { get; set; }

        [JsonProperty("startLocation")]
        public Location StartLocation { get; set; }

        [JsonProperty("endLocation")]
        public Location EndLocation { get; set; }

        [JsonProperty("navigationInstruction")]
        public NavigationInstruction NavigationInstruction { get; set; }
    }
}
