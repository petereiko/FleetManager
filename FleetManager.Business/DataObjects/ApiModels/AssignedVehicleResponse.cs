using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ApiModels
{
    public class AssignedVehicleResponse
    {
        public long VehicleId { get; set; }
        public string MakeModel { get; set; } = string.Empty;
        public string PlateNo { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public string? MainImageUrl { get; set; }
    }
}
