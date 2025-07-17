using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsDto
{
    public class ExportRequest
    {
        public string ReportType { get; set; }
        public ReportRequest ReportRequest { get; set; }
        public ExportFormat Format { get; set; }
    }
}
