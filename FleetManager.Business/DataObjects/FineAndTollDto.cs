using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects
{
    public class FineAndTollDto
    {
        public long Id { get; set; }
        public long VehicleId { get; set; }
        public string VehicleDescription { get; set; } = "";
        public string DriverId { get; set; } = "";
        public string DriverName { get; set; } = "";
        public FineTollType Type { get; set; }
        public string Title { get; set; } = "";
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "NGN";
        public string Reason { get; set; } = "";
        public string Notes { get; set; } = "";
        public FineTollStatus Status { get; set; }
        public DateTime? PaidDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = "";
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public long? CompanyBranchId { get; set; }
        public bool IsMinimal { get; set; } = false;
    }

}
