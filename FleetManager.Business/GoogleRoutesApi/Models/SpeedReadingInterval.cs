using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.GoogleRoutesApi.Models
{
    public class SpeedReadingInterval
    {
        [JsonProperty("startPolylinePointIndex")]
        public int StartPolylinePointIndex { get; set; }

        [JsonProperty("endPolylinePointIndex")]
        public int EndPolylinePointIndex { get; set; }

        [JsonProperty("speed")]
        public string Speed { get; set; }
    }
}
