using FleetManager.Business.DataObjects.RepairHistoryDto;
using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.RepairDto
{
    public class RepairDto
    {
        public long Id { get; set; }
        public long VehicleId { get; set; }
        public string VehicleDescription { get; set; } = "";
        public long? DriverId { get; set; }
        public string? DriverName { get; set; }
        public string Subject { get; set; } = "";
        public string? Notes { get; set; }
        public RepairStatus Status { get; set; }
        public MaintenancePriority Priority { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolvedBy { get; set; }
        public List<RepairItemDto> Items { get; set; } = new();
        public RepairInvoiceDto? Invoice { get; set; }


        // company / branch fields (new)
        public string? CompanyName { get; set; }
        public string? CompanyLogoUrl { get; set; }
        public string? CompanyEmail { get; set; }
        public string? CompanyPhone { get; set; }
        public string? BranchName { get; set; }
        public string? BranchAddress { get; set; }
        public string? BranchState { get; set; }
        public string? BranchPhone { get; set; }
        public string? BranchEmail { get; set; }
        public bool IsBranchHeadOffice { get; set; }
        public string? CompanyLogoDataUrl { get; set; }
    }

}
