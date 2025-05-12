using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class BaseEntity
    {
        public long Id { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public long? CompanyId { get; set; }
        public string? CreatedBy { get; set; }
    }
}
