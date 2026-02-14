using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ApiModels
{
    public class MaintenanceTicketRequest
    {
        [Required]
        public long VehicleId { get; set; }

        [Required]
        [StringLength(150)]
        public string Subject { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Notes { get; set; }

        [Required]
        public string Priority { get; set; } = "Moderate"; // "Low", "Moderate", "High", "Urgent"

        [Required]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        public List<MaintenanceTicketItemRequest> Items { get; set; } = new();
    }
}
