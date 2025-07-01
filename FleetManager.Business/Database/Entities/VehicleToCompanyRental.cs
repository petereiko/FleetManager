using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class VehicleToCompanyRental:BaseEntity 
    {
        public long VehicleId { get; set; }
        public virtual Vehicle Vehicle { get; set; }

        public long CompanyId { get; set; }
        public virtual Company Company { get; set; }

        public RentalStatus RentalStatus { get; set; }

        public int VehicleCount { get; set; }
        public string? Comment { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? ActualReturnedDate { get; set; }

        public RentalDocumentType DocumentType { get; set; }
        public string? RentalRequestFileName { get; set; }

        public string? RentalRequestFilePath { get; set; }

        public string? RentalAgreementFileName { get; set; }

        public string? RentalAgreementFilePath { get; set; }
        // …any rental-specific fields…
    }
    
}
