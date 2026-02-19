using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ApiModels
{
    public class FineTollResponse
    {
        public long Id { get; set; }
        public long VehicleId { get; set; }
        public string VehicleDescription { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool IsMinimal { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? PaidDate { get; set; }
        public DateTime DateLogged { get; set; }
    }
}
