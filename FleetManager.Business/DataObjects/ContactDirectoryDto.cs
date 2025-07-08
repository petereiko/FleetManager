using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects
{
    public class ContactDirectoryDto
    {
        public long Id { get; set; }

        public string ContactName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string VendorName { get; set; }
        public string? Address { get; set; }       
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }

        public string? Services { get; set; }
        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate {  get; set; }
    }

}
