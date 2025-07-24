using FleetManager.Business.DataObjects;
using FleetManager.Business.DataObjects.MaintenanceDto;
using FleetManager.Business.DataObjects.VehicleDto;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.MaintenanceViewModels
{
    public class MaintenanceTicketListViewModel
    {
        public List<MaintenanceTicketDto> Tickets { get; set; } = new();
        public PaginationDto Pagination { get; set; } = new();
        // filter state
        public string CurrentFilter { get; set; } = "ByBranch";
        public long? DriverId { get; set; }
        public long? VehicleId { get; set; }

        // for dropdowns
        public List<SelectListItem> Drivers { get; set; } = new();
        public List<SelectListItem> Vehicles { get; set; } = new();

        public List<SelectListItem> FilterOptions { get; } = new();
        public MaintenanceTicketListViewModel()
        {
            FilterOptions = new List<SelectListItem> {
            new("Branch","ByBranch"),
            new("Driver","ByDriver"),
            new("Vehicle","ByVehicle")
        };
        }

        public MaintenanceTicketStatusEditViewModel EditModel { get; set; }
    }
}
