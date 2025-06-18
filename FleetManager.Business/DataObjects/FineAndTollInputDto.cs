using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects
{
    public class FineAndTollInputDto
    {
        public long VehicleId { get; set; }
        public FineTollType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "NGN";
        public string Reason { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool IsMinimal { get; set; } = false;
    }
}
