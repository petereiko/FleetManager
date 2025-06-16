using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.GoogleRoutesApi.Models
{
    public class TollInfo
    {
        [JsonProperty("estimatedPrice")]
        public List<Money> EstimatedPrice { get; set; } = new List<Money>();
    }
}
