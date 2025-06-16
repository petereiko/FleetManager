using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.GoogleRoutesApi.Models
{
    public class FallbackInfo
    {
        [JsonProperty("reason")]
        public string Reason { get; set; }
    }
}
