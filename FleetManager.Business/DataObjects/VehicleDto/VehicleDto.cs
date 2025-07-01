using FleetManager.Business.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.VehicleDto
{
    public class VehicleDto
    {
        public long? Id { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }

        // ↓ NEW ↓
        public int VehicleMakeId { get; set; }
        public int VehicleModelId { get; set; }
        public int Year { get; set; }
        public string VIN { get; set; }
        public string PlateNo { get; set; }
        public string Color { get; set; }
        public string? EngineNumber { get; set; }
        public string? ChassisNumber { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public DateTime? LastServiceDate { get; set; }
        public int? Mileage { get; set; }
        public FuelType FuelType { get; set; }
        public TransmissionType TransmissionType { get; set; }
        public string? InsuranceCompany { get; set; }
        public DateTime? InsuranceExpiryDate { get; set; }
        public DateTime? RoadWorthyExpiryDate { get; set; }
        public long CompanyBranchId { get; set; }
        public VehicleStatus VehicleStatus { get; set; }
        public VehicleType VehicleType { get; set; }

        public List<IFormFile> PhotoFiles { get; set; } = new();
        public List<IFormFile> DocumentFiles { get; set; } = new();

        public List<VehicleDocumentDto> Photos { get; set; } = new();
        public List<VehicleDocumentDto> Documents { get; set; } = new();



        // Filled on GET(Edit)  
        public List<VehicleDocumentDto> ExistingImages { get; set; } = new();
        public List<VehicleDocumentDto> ExistingDocuments { get; set; } = new();
    }

}
