using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.GoogleMap.Options
{
    public class GoogleRoutesApiOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://routes.googleapis.com";
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}
