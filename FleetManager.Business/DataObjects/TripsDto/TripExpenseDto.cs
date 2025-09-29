using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.TripsDto
{
    public class TripExpenseDto
    {
        public long Id { get; set; }
        public long TripId { get; set; }
        public ExpenseType ExpenseType { get; set; }
        public string ExpenseTypeDisplay { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string? ReceiptFileName { get; set; }
        public string? ReceiptUrl { get; set; }
        public bool IsVerified { get; set; }
        public string? VerifiedBy { get; set; }
        public DateTime? VerificationDate { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
