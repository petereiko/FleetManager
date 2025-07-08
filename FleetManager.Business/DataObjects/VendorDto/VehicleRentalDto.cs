using FleetManager.Business.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.VendorDto
{
    public class VehicleRentalDto
    {
        public long Id { get; set; }
        public long VendorId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public long? CompanyBranchId { get; set; }
        public string? BranchName { get; set; } = string.Empty;
        public RentalStatus RentalStatus { get; set; }
        public int VehicleCount { get; set; }
        public string? Comment { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? ActualReturnedDate { get; set; }
        public string? RequestFilePath { get; set; }
        public IFormFile? AgreementFile { get; set; }
        public string? AgreementFilePath { get; set; }
    }
}
