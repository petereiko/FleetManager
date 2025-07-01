using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.VehicleDto
{
    public class VehicleMakeResponseDto
    {
        public int Count { get; set; }
        public string Message { get; set; }
        public object SearchCriteria { get; set; }
        public List<MakeDto> Results { get; set; } = new();
    }

    public class MakeDto
    {
        [JsonProperty("Make_ID")]    
        public int MakeID { get; set; }

        [JsonProperty("Make_Name")]
        public string MakeName { get; set; }

        [JsonProperty("Model_ID")]
        public int ModelID { get; set; }

        [JsonProperty("Model_Name")]
        public string ModelName { get; set; }
    }
}
