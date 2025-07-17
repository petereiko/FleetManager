using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using FleetManager.Business.Enums;

namespace FleetManager.Business.Database.Entities
{
    public class Vehicle : BaseEntity
    {
        public int? VehicleMakeId { get; set; }
        public virtual VehicleMake VehicleMake { get; set; }
        public int? VehicleModelId { get; set; }
        public virtual VehicleModel VehicleModel { get; set; }

        //public string Make { get; set; }
        //public string Model { get; set; }
        public int Year { get; set; }
        public string VIN { get; set; } // Vehicle Identification Number
        public string PlateNo { get; set; }
        public string Color { get; set; }
        public string? EngineNumber { get; set; }
        public string? ChassisNumber { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public DateTime? LastServiceDate { get; set; }
        public int? Mileage { get; set; }
        public FuelType FuelType { get; set; }
        public TransmissionType TransmissionType { get; set; } // Manual/Automatic
        public string? InsuranceCompany { get; set; }
        public DateTime? InsuranceExpiryDate { get; set; }
        public DateTime? RoadWorthyExpiryDate { get; set; }
        public long? CompanyId { get; set; }
        public virtual Company Company { get; set; }
        public long? CompanyBranchId { get; set; }
        public virtual CompanyBranch? CompanyBranch { get; set; }
        public VehicleStatus VehicleStatus { get; set; } // Active, Inactive, Under Maintenance
        public VehicleType VehicleType { get; set; }
        public List<VehicleDocument> VehicleDocuments { get; set; } = new List<VehicleDocument>();


        public long? VendorId { get; set; }
        public virtual Vendor? Vendor { get; set; }
        public virtual ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();

    }
}




