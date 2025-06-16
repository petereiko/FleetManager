using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.GoogleRoutesApi.Models
{
    public class Waypoint
    {
        [JsonProperty("location")]
        public Location Location { get; set; }

        [JsonProperty("placeId")]
        public string PlaceId { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("via")]
        public bool Via { get; set; } = false;

        [JsonProperty("sideOfRoad")]
        public bool SideOfRoad { get; set; } = false;
    }
}
