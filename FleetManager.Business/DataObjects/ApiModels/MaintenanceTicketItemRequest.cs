using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ApiModels
{
    public class MaintenanceTicketItemRequest
    {
        [Required]
        public int PartCategoryId { get; set; }

        [Required]
        public int PartId { get; set; }

        [StringLength(500)]
        public string? CustomDescription { get; set; }

        [Required]
        [Range(1, 1000)]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, 1000000)]
        public decimal UnitPrice { get; set; }
    }
}
