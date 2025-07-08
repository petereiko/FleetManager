using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.VendorDto
{
    public class VehicleRentalApplyViewModel
    {
        public long VendorId { get; set; }
        public string VendorName { get; set; } = "";
        public long CompanyBranchId { get; set; }
        public string CompanyBranchName { get; set; } = "";

        // embedded DTO for form post
        public VehicleRentalOnboardingDto Rental { get; set; } = new();
    }
}
