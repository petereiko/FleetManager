using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class TripExpense : BaseEntity
    {
        public long TripId { get; set; }
        public virtual Trip Trip { get; set; }

        public ExpenseType ExpenseType { get; set; } // Fuel, Toll, Parking, Maintenance, Others
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "NGN";
        public DateTime ExpenseDate { get; set; }

        public string? ReceiptFileName { get; set; }
        public string? ReceiptUrl { get; set; }

        public bool IsVerified { get; set; }
        public string? VerifiedBy { get; set; }
        public DateTime? VerificationDate { get; set; }
    }
}
