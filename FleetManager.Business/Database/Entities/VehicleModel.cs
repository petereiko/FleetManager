using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class VehicleModel
    {

        [Key]
        public int VehicleModelId { get; set; }
        public int VehicleMakeId { get; set; }
        public string Name { get; set; }
    }
}
