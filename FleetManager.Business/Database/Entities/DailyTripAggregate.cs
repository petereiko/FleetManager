using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class DailyTripAggregate:BaseEntity
    {

        // UTC date (midnight UTC for the day)
        public DateTime DayUtc { get; set; }

        public int TotalTrips { get; set; }
        public int Scheduled { get; set; }
        public int Assigned { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
        public int Cancelled { get; set; }

        // aggregated metrics
        public decimal TotalDistance { get; set; }
        public decimal TotalFuelCost { get; set; }

        // branch & company scope
        public long CompanyBranchId { get; set; }
        public virtual CompanyBranch CompanyBranch { get; set; }
        public long CompanyId { get; set; }
        public virtual Company Company { get; set; }

        public DateTime? ComputedDate { get; set; } // when this aggregate was computed
    }
}
