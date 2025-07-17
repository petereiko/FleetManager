using ClosedXML.Excel;
using FleetManager.Business.DataObjects.ReportsDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.ReportModule;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.ReportModule
{
    public class ReportExportService : IReportExportService
    {
        public async Task<byte[]> ExportReportAsync<T>(ReportResult<T> reportData, ExportFormat format)
        {
            return format switch
            {
                ExportFormat.Excel => GenerateExcelReport(reportData),
                ExportFormat.PDF => GeneratePdfReport(reportData),
                ExportFormat.CSV => GenerateCsvReport(reportData),
                _ => throw new ArgumentException("Unsupported export format")
            };
        }

        private byte[] GenerateExcelReport<T>(ReportResult<T> reportData)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Report");

            // Add headers (row 1)
            var properties = typeof(T).GetProperties();
            for (int col = 0; col < properties.Length; col++)
            {
                worksheet.Cell(1, col + 1).Value = properties[col].Name;
            }

            // Add data (rows 2+)
            for (int row = 0; row < reportData.Data.Count; row++)
            {
                for (int col = 0; col < properties.Length; col++)
                {
                    worksheet.Cell(row + 2, col + 1).Value =
                        properties[col].GetValue(reportData.Data[row])?.ToString() ?? "";
                }
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        private byte[] GeneratePdfReport<T>(ReportResult<T> reportData)
        {
            using var stream = new MemoryStream();
            using var document = new iTextSharp.text.Document();
            var writer = iTextSharp.text.pdf.PdfWriter.GetInstance(document, stream);

            document.Open();
            document.Add(new iTextSharp.text.Paragraph(reportData.ReportType));

            // Add table
            var table = new iTextSharp.text.pdf.PdfPTable(
                typeof(T).GetProperties().Length);

            // Add header
            foreach (var prop in typeof(T).GetProperties())
            {
                table.AddCell(prop.Name);
            }

            // Add data
            foreach (var item in reportData.Data)
            {
                foreach (var prop in typeof(T).GetProperties())
                {
                    table.AddCell(prop.GetValue(item)?.ToString() ?? string.Empty);
                }
            }

            document.Add(table);
            document.Close();
            return stream.ToArray();
        }

        private byte[] GenerateCsvReport<T>(ReportResult<T> reportData)
        {
            using var writer = new StringWriter();
            using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(reportData.Data);
            return Encoding.UTF8.GetBytes(writer.ToString());
        }

        public Task<List<ReportMetadata>> GetAvailableReportsAsync()
        {
            return Task.FromResult(new List<ReportMetadata>
        {
            new() { Name = "Driver Performance", Type = "DriverPerformance" },
            new() { Name = "Driver Compliance", Type = "DriverCompliance" },
            new() { Name = "Vehicle Utilization", Type = "VehicleUtilization" },
            new() { Name = "Vehicle Maintenance", Type = "VehicleMaintenance" },
            new() { Name = "Fuel Consumption", Type = "FuelConsumption" },
            new() { Name = "Fuel Efficiency", Type = "FuelEfficiency" },
            new() { Name = "Vehicle Assignments", Type = "VehicleAssignment" }
        });
        }
    }
}
