using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsDto
{
    // ReportsDto/ReportHeaderModel.cs
    public class ReportHeaderModel
    {
        public string ReportType { get; set; }
        public int TotalRecords { get; set; }
        public DateTime GeneratedAt { get; set; }
        public DateTime? StartDate { get; set; }  // From ReportRequest
        public DateTime? EndDate { get; set; }
        public ReportRequest? ReportRequest { get; set; }
    }

}
