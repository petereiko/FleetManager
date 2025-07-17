using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsDto
{
    public class ReportRequest
    {
        public string ReportType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        public Dictionary<string, object> Filters { get; set; } = new();
        public int PageSize { get; set; } = 50;
        public int PageNumber { get; set; } = 1;
        public ExportFormat? ExportFormat { get; set; }
        public string? Status { get; set; } // OnDuty, OffDuty, Active, Inactive, etc.
        public bool ExportOnFilterApply { get; set; } = false;
    }
}
