using FleetManager.Business.Database.Entities;
using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsDto
{
    public class DriverPerformanceReportDto
    {
        public long DriverId { get; set; }
        public string DriverName { get; set; }
        public string LicenseNumber { get; set; }
        public DateTime? LicenseExpiryDate { get; set; }
        public bool IsLicenseExpiringSoon { get; set; }
        public ShiftStatus CurrentShiftStatus { get; set; }
        public DateTime? LastSeen { get; set; }
        public int TotalTrips { get; set; }
        public decimal TotalDistance { get; set; }
        public decimal TotalFuelCost { get; set; }
        public decimal AverageFuelConsumption { get; set; }
        public int ActiveDays { get; set; }
        public decimal PerformanceScore { get; set; }
        public List<string> VehiclesAssigned { get; set; } = new();
        public CompanyBranch Branch { get; set; }
    }
}
