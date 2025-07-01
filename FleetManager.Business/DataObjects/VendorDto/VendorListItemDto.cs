using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.VendorDto
{
    public class VendorListItemDto
    {
        public int Id { get; set; }
        public string VendorName { get; set; } = null!;
        public string VendorCategoryName { get; set; } = null!;
        public string VendorServiceName { get; set; } = null!;
        public bool IsActive { get; set; }
    }
}
