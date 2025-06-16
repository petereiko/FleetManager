using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.GoogleRoutesApi.Models
{
    public class DirectionsResponse
    {
        [JsonProperty("routes")]
        public List<Route> Routes { get; set; } = new List<Route>();
    }
}
