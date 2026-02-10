using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ApiModels
{
    public class VehicleDetailsResponse
    {
        public long VehicleId { get; set; }
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public string PlateNo { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public DateTime? RegistrationDate { get; set; }
        public DateTime? LastServiceDate { get; set; }
        public int? Mileage { get; set; }
        public string FuelType { get; set; } = string.Empty;
        public string TransmissionType { get; set; } = string.Empty;
        public string VehicleStatus { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public List<VehiclePhotoDto> Photos { get; set; } = new();

        // Assignment details
        public DateTime? AssignmentStartDate { get; set; }
        public DateTime? AssignmentEndDate { get; set; }
    }
}
