using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ApiModels
{
    public class FuelLogResponse
    {
        public long? Id { get; set; }
        public long VehicleId { get; set; }
        public string VehicleDescription { get; set; } = string.Empty;
        public string LicenseNo { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int? Odometer { get; set; }
        public decimal Volume { get; set; }
        public decimal Cost { get; set; }
        public string FuelType { get; set; } = string.Empty;
        public string? ReceiptUrl { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
