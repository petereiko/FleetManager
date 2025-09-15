using FleetManager.Business.DataObjects.RepairDto;
using FleetManager.Business.DataObjects.VehicleDto;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.RepairHistoryViewModels
{
    public class RepairListViewModel
    {
        public IEnumerable<RepairDto> Repairs { get; set; } = Array.Empty<RepairDto>();

        public PaginationDto Pagination { get; set; } = new PaginationDto();

        // Filtering / UI state
        public string CurrentFilter { get; set; } = "ByBranch";
        public long? DriverId { get; set; }
        public long? VehicleId { get; set; }

        // Dropdowns for filter
        public List<SelectListItem> Drivers { get; set; } = new();
        public List<SelectListItem> Vehicles { get; set; } = new();

        // Inline status edit model used by the index page (AJAX)
        public RepairStatusEditViewModel EditModel { get; set; } = new RepairStatusEditViewModel();
    }
}
