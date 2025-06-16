using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.GoogleViewModels
{
    public class MapViewModel
    {
        public string EncodedPolyline { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public string OriginAddress { get; set; } = "";

        public string DestinationAddress { get; set; } = "";
    }
}
