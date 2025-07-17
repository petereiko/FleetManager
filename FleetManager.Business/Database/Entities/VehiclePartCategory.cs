using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class VehiclePartCategory:BaseEntity
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        public ICollection<VehiclePart> VehicleParts { get; set; } = new List<VehiclePart>();
    }

}
