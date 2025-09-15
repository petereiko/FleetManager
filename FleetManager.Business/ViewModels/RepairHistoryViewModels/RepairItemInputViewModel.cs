using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.RepairHistoryViewModels
{
    public class RepairItemInputViewModel
    {
        public long? Id { get; set; }

        [Display(Name = "Part Category")]
        public int? PartCategoryId { get; set; }

        [Display(Name = "Part")]
        public int? PartId { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? CustomDescription { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; } = 1;

        [Range(0, double.MaxValue, ErrorMessage = "Unit price must be >= 0")]
        public decimal UnitPrice { get; set; } = 0m;
    }
}
