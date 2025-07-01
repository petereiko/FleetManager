using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.VehicleDto
{
    class VehicleModelDto
    {
        public int Id { get; set; }
        public int VehicleModelId { get; set; }
        public int VehicleMakeId { get; set; }
        public string Name { get; set; }
    }
}
