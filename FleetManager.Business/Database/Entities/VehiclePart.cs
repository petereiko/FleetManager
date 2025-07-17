using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class VehiclePart:BaseEntity
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        [Required]
        public int? VehiclePartCategoryId { get; set; }
        public virtual VehiclePartCategory VehiclePartCategory { get; set; } = null!;
        public bool IsActive { get; set; } = true;
    }

}
