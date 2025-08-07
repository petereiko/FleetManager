using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels
{
    public class AssignedVehicleViewModel
    {
        public string EncodedVehicleId { get; set; } = string.Empty;
        public string VehicleMakeModel { get; set; }
        public string PlateNo { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        //public string? MainImagePath { get; set; }
    }
}
