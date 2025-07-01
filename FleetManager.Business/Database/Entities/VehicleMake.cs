using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class VehicleMake
    {
        [Key]
        public int VehicleMakeId { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }

}
