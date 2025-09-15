using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities.RepairHistory
{
    // Repair.cs
    public class Repair : BaseEntity
    {
        public long? CompanyId { get; set; }
        public virtual Company? Company { get; set; }

        public long? CompanyBranchId { get; set; }
        public virtual CompanyBranch? CompanyBranch { get; set; }

        public long VehicleId { get; set; }
        public virtual Vehicle Vehicle { get; set; } = null!;

        public long? DriverId { get; set; }
        public virtual Driver? Driver { get; set; }

        public string Subject { get; set; } = string.Empty;
        public string? Notes { get; set; }

        public RepairStatus Status { get; set; } = RepairStatus.Pending;
        public MaintenancePriority Priority { get; set; } = MaintenancePriority.Moderate; 
        
        public DateTime? ResolvedAt { get; set; }

        public virtual ICollection<RepairItem> Items { get; set; } = new List<RepairItem>();
        //public long? RepairInvoiceId { get; set; }
        public virtual RepairInvoice? Invoice { get; set; }
        
    }

}
