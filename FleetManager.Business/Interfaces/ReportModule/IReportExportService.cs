using FleetManager.Business.DataObjects.ReportsDto;
using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.ReportModule
{
    public interface IReportExportService
    {
        Task<byte[]> ExportReportAsync<T>(ReportResult<T> reportData, ExportFormat format);
        Task<List<ReportMetadata>> GetAvailableReportsAsync();
    }
}
