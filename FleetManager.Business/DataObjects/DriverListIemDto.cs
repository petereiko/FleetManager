using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects
{
    public class DriverListItemDto
    {
        public long Id { get; set; }
        public string FullName { get; set; }
        public string LicenseNumber { get; set; }
        public DateTime? LicenseExpiryDate { get; set; }
        public ShiftStatus ShiftStatus { get; set; }
        public string PhotoPath { get; set; }    // e.g. “/DriverPhotos/…jpg”
        public bool IsActive { get; set; }
        public string BranchName { get; set; }   // for global users
        public EmploymentStatus EmploymentStatus { get; set; }
        public DateTime CreatedDate { get; set; }

    }
}
