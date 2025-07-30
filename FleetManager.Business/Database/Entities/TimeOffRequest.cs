using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class TimeOffRequest : BaseEntity
    {
        public long DriverId { get; set; }
        public virtual Driver Driver { get; set; } = null!;

        public long CompanyBranchId { get; set; }
        public virtual CompanyBranch CompanyBranch { get; set; } = null!;

        public long CategoryId { get; set; }
        public virtual TimeOffCategory Category { get; set; } = null!;

        [Required, DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        public TimeOffStatus Status { get; set; } = TimeOffStatus.Pending;

        // Audit
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? AdminNotes { get; set; }
    }
}
