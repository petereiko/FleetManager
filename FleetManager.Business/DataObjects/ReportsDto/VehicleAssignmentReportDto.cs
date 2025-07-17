using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsDto
{
    public class VehicleAssignmentReportDto
    {
        public long AssignmentId { get; set; }
        public string DriverName { get; set; }
        public string VehicleIdentifier { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int AssignmentDuration { get; set; }
        public bool IsActive { get; set; }
        public decimal TotalFuelCost { get; set; }
        public double TotalDistance { get; set; }
        public int TotalTrips { get; set; }
        public decimal PerformanceScore { get; set; }
    }
}
