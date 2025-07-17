using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsDto
{
    public class VehicleUtilizationReportDto
    {
        public long VehicleId { get; set; }
        public string VehicleIdentifier { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public string PlateNo { get; set; }
        public VehicleStatus Status { get; set; }
        public int TotalTrips { get; set; }
        public double TotalDistance { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public decimal TotalFuelCost { get; set; }
        public decimal CostPerKm { get; set; }
        public DateTime? LastServiceDate { get; set; }
        public int DaysSinceLastService { get; set; }
        public int? CurrentMileage { get; set; }
        public string CurrentDriverName { get; set; }
    }
}
