using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.GoogleRoutesApi.Models
{
    public class NavigationInstruction
    {
        [JsonProperty("maneuver")]
        public string Maneuver { get; set; }

        [JsonProperty("instructions")]
        public string Instructions { get; set; }
    }
}
