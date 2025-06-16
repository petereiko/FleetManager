using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.GoogleRoutesApi.Models
{
    public class TravelAdvisory
    {
        [JsonProperty("tollInfo")]
        public TollInfo TollInfo { get; set; }

        [JsonProperty("speedReadingIntervals")]
        public List<SpeedReadingInterval> SpeedReadingIntervals { get; set; } = new List<SpeedReadingInterval>();
    }
}
