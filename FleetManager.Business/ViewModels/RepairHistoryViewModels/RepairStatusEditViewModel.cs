using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.RepairHistoryViewModels
{
    public class RepairStatusEditViewModel
    {
        [Required]
        public long RepairId { get; set; }

        [Required]
        public RepairStatus NewStatus { get; set; }

        public InvoiceStatus? NewInvoiceStatus { get; set; }

        public string? AdminNotes { get; set; }
    }
}
