using FleetManager.Business.DataObjects.RepairDto;
using FleetManager.Business.DataObjects.VehicleDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels
{
    public class RepairHistoryViewModel
    {
        public long VehicleId { get; set; }
        public string VehicleDescription { get; set; } = string.Empty;

        // Repairs returned from IRepairService.QueryRepairsByVehicleAsync
        public IEnumerable<RepairDto> Repairs { get; set; } = Array.Empty<RepairDto>();

        public PaginationDto Pagination { get; set; } = new PaginationDto();
    }

}
