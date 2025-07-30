using FleetManager.Business.Database.Entities;
using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.Schedule
{
    public class TimeOffRequestDto
    {
        public long Id { get; set; }
        public long DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public long CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public long CompanyBranchId { get; set; }
        public string CompanyBranch { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Reason { get; set; }
        public TimeOffStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public string? RequestedBy { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? AdminNotes { get; set; }
    }
}
