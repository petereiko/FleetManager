using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.VendorDto
{
    public class VehicleRentalAgreementDto
    {
        public long Id { get; set; }
        public IFormFile? RentalAgreementFile { get; set; }
    }
}
