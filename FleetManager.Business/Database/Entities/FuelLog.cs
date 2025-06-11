using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class FuelLog : BaseEntity
    {
        public long? DriverId { get; set; }
        public virtual Driver? Driver { get; set; }

        public long? VehicleId { get; set; }
        public virtual Vehicle? Vehicle { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;

        /// <summary>Odometer reading at time of fill (km or miles).</summary>
        public int? Odometer { get; set; }

        /// <summary>Liters or gallons refueled.</summary>
        public decimal Volume { get; set; }

        /// <summary>Total cost for this fill.</summary>
        public decimal Cost { get; set; }

        public FuelType FuelType { get; set; }

        /// <summary>Optional link to a scanned receipt image.</summary>
        public string? ReceiptPath { get; set; }

        /// <summary>Any notes (e.g. location, remarks).</summary>
        public string? Notes { get; set; }
    }

}
