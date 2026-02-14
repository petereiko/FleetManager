using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ApiModels.GoogleRoutes
{
    public class RouteResponse
    {
        public List<RouteOption> Routes { get; set; } = new List<RouteOption>();
        public string Status { get; set; } = string.Empty;
    }
}
