using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.VendorDto
{
    public class VehicleRentalOnboardingDto
    {
        public long? CompanyBranchId {  get; set; }
        public long VendorId { get; set; }
        public int VehicleCount { get; set; }
        public string? Comment { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public IFormFile? RequestFile { get; set; }
    }
}
