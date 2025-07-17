using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsDto
{
    public class ReportResult<T> : ReportResultBase
    {
        public List<T> Data { get; set; } = new();
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public Dictionary<string, object> Summary { get; set; } = new();
        public string ReportType { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // ✅ Add this property:
        public ReportRequest? Request { get; set; } = new();
    }
}
