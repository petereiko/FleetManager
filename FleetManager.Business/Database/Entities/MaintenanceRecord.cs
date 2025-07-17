using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class MaintenanceRecord : BaseEntity
    {
        public long VehicleId { get; set; }
        public virtual Vehicle Vehicle { get; set; } = null!;
        public long? DriverId { get; set; }
        public virtual Driver? Driver { get; set; }
        public MaintenanceType MaintenanceType { get; set; } = MaintenanceType.Corrective;
        public string Description { get; set; } = null!;
        public string? Notes { get; set; }
        public DateTime ReportedDate { get; set; } = DateTime.UtcNow;
        public bool IsUrgent { get; set; }
        public bool IsResolved { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public decimal? Cost { get; set; }

        /// <summary>Path to any uploaded document (e.g., invoice, image)</summary>
        //public string? AttachmentPath { get; set; }

        /// <summary>Who created this record (Driver, Admin, System)</summary>
        public string CreatedBy { get; set; } /*= "Driver";*/
    }

}
