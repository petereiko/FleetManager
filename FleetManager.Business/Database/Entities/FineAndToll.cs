using FleetManager.Business.Database.IdentityModels;
using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class FineAndToll:BaseEntity
    {
        public FineTollType Type { get; set; }
        public string Title { get; set; }
        public DateTime? PaidDate { get; set; }
        public decimal Amount { get; set; }
        public bool IsMinimal { get; set; } = false;
        public string Currency { get; set; }
        public string Reason { get; set; }
        public string Notes { get; set; }
        public FineTollStatus Status { get; set; } = FineTollStatus.Unpaid;
        public string DriverId { get; set; }
        public virtual ApplicationUser Driver { get; set; }
        public long VehicleId { get; set; }
        public virtual Vehicle Vehicle { get; set; }
        public  long? CompanyBranchId { get; set; }
        public virtual CompanyBranch CompanyBranch { get; set; }

    }
}
