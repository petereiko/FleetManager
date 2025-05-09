using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FleetManager.Business.Enums;

namespace FleetManager.Business.Database.Entities
{
    public class NextOfKin:BaseEntity
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public RelationshipType RelationshipType { get; set; }
        public long? DriverId { get; set; }
        public virtual Driver? Driver { get; set; }
    }
}
