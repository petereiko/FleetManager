using FleetManager.Business.DataObjects.MaintenanceDto;
using FleetManager.Business.DataObjects.VehicleDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.MaintenanceViewModels
{
    public class InvoiceListViewModel
    {
        public List<InvoiceDto> Invoices { get; set; } = new();
        public PaginationDto Pagination { get; set; } = new();
    }
}
