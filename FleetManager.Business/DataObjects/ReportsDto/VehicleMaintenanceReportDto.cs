using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsDto
{
    public class VehicleMaintenanceReportDto
    {
        public long VehicleId { get; set; }
        public string VehicleIdentifier { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string PlateNo { get; set; }
        public DateTime? LastServiceDate { get; set; }
        public int DaysSinceLastService { get; set; }
        public int? CurrentMileage { get; set; }
        public DateTime? InsuranceExpiryDate { get; set; }
        public DateTime? RoadWorthyExpiryDate { get; set; }
        public bool IsInsuranceExpiringSoon { get; set; }
        public bool IsRoadWorthyExpiringSoon { get; set; }
        public bool RequiresService { get; set; }
        public List<string> MaintenanceAlerts { get; set; } = new();
        public VehicleStatus Status { get; set; }
    }
}
