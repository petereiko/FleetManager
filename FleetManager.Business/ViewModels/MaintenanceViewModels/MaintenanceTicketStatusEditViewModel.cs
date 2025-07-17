using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.MaintenanceViewModels
{
    public class MaintenanceTicketStatusEditViewModel
    {
        [Required]
        public long TicketId { get; set; }

        [Display(Name = "Current Status")]
        public TicketStatus CurrentStatus { get; set; }

        [Required]
        [Display(Name = "New Status")]
        public TicketStatus NewStatus { get; set; }

        [Display(Name = "Admin Notes")]
        public string? AdminNotes { get; set; }

        [Display(Name = "Invoice Status (if any)")]
        public InvoiceStatus? NewInvoiceStatus { get; set; }
    }
}
