using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class CompanyBranch:BaseEntity
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? ManagerName { get; set; }
        public string? ManagerPhone { get; set; }
        public string? ManagerEmail { get; set; }
        public long? CompanyId { get; set; }
        public virtual Company? Company { get; set; }
        public bool IsHeadOffice { get; set; }
        public string? Notes { get; set; }
        public long? StateId { get; set; }
        public long? LgaId { get; set; }
        
    }
}
