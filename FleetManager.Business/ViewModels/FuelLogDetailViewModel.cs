using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels
{
    public class FuelLogDetailViewModel
    {
        public long Id { get; set; }
        public DateTime Date { get; set; }
        public string DriverName { get; set; } = "";
        public string VehicleDescription { get; set; } = "";
        public double Odometer { get; set; }
        public double Volume { get; set; }
        public decimal Cost { get; set; }
        public string FuelType { get; set; } = "";
        public string? ReceiptPath { get; set; }
        public string? Notes { get; set; }
    }
}
