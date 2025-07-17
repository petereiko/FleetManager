using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.MaintenanceViewModels
{
    public class MaintenanceTicketItemViewModel
    {
        [Required]
        [Display(Name = "Part Category")]
        public int PartCategoryId { get; set; }

        [Required]
        [Display(Name = "Part")]
        public int PartId { get; set; }

        [StringLength(200)]
        [Display(Name = "Description")]
        public string? CustomDescription { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0.0, double.MaxValue)]
        [DataType(DataType.Currency)]
        public decimal UnitPrice { get; set; }
    }

}
