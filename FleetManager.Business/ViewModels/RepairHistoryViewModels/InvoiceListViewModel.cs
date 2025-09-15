using FleetManager.Business.DataObjects.RepairHistoryDto;
using FleetManager.Business.DataObjects.VehicleDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.RepairHistoryViewModels
{
    public class InvoiceListViewModel
    {
        // Collection of invoices returned by QueryRepairInvoicesByBranchAsync
        public IEnumerable<RepairInvoiceDto> Invoices { get; set; } = Array.Empty<RepairInvoiceDto>();
        public PaginationDto Pagination { get; set; } = new PaginationDto();
    }
}
