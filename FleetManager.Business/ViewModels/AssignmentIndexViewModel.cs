using FleetManager.Business.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels
{
    public class AssignmentIndexViewModel
    {
        public long? DriverFilterId { get; set; }
        public List<DriverVehicleListItemDto> Assignments { get; set; } = new();
        public List<AssignedVehicleViewModel> AssignedVehicles { get; set; }

    }
}
