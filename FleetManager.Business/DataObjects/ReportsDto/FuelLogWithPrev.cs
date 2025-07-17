using FleetManager.Business.Database.Entities;
using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsDto
{
    public class FuelLogWithPrev
    {
        public long Id { get; set; }

        public long? VehicleId { get; set; }
        public long? DriverId { get; set; }

        public DateTime Date { get; set; }

        public decimal? Volume { get; set; }
        public decimal? Cost { get; set; }

        public int? Odometer { get; set; }
        public int? PreviousOdometer { get; set; }

        public FuelType FuelType { get; set; } // Enum assumed from your existing model

        public string? Notes { get; set; }

        public long CompanyBranchId { get; set; }

        public string VehicleMake { get; set; } = string.Empty;
        public string VehicleModel { get; set; } = string.Empty;
        public string VehicleIdentifier { get; set; } = string.Empty;

        public string? DriverName { get; set; }
        //public virtual Vehicle? Vehicle { get; set; }
        //public virtual Driver? Driver { get; set; }
    }

}
