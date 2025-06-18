using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.GoogleRoutesApi.Models
{
    public class RouteModifiers
    {
        [JsonProperty("avoidTolls")]
        public bool AvoidTolls { get; set; } = false;

        [JsonProperty("avoidHighways")]
        public bool AvoidHighways { get; set; } = false;

        [JsonProperty("avoidFerries")]
        public bool AvoidFerries { get; set; } = false;
    }

}
