using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.VehicleDto
{
    public class VehicleFilterDto
    {
        public long? BranchId { get; set; }
        public VehicleStatus? Status { get; set; }
        public VehicleType? Type { get; set; }
        public string? Search { get; set; }
    }
}
