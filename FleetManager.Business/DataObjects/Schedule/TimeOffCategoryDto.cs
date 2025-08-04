using FleetManager.Business.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.Schedule
{
    public class TimeOffCategoryDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public long? CompanyBranchId { get; set; }
        public string?  BranchName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public string? Description { get; set; }
    }
}
