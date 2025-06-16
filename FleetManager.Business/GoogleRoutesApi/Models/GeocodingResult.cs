using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.GoogleRoutesApi.Models
{
    public class GeocodingResult
    {
        [JsonProperty("placeId")]
        public string PlaceId { get; set; }

        [JsonProperty("types")]
        public List<string> Types { get; set; } = new List<string>();
    }
}
