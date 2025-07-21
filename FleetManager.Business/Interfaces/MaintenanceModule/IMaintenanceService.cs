using FleetManager.Business.DataObjects.MaintenanceDto;
using FleetManager.Business.Enums;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.MaintenanceModule
{
    public interface IMaintenanceService
    {
        // Tickets with pagination
        Task<MessageResponse<PaginatedResult<MaintenanceTicketDto>>> QueryTicketsByBranchAsync(int page, int pageSize, long? branchId = null);
        Task<MessageResponse<PaginatedResult<MaintenanceTicketDto>>> QueryTicketsByDriverAsync(int page, int pageSize, long? driverId);
        Task<MessageResponse<PaginatedResult<MaintenanceTicketDto>>> QueryTicketsByVehicleAsync(int page, int pageSize, long vehicleId);
        Task<MaintenanceTicketDto?> GetTicketByIdAsync(long ticketId);
        Task<MessageResponse<MaintenanceTicketDto>> CreateTicketAsync(MaintenanceTicketInputDto input, string createdByUserId);
        Task<MessageResponse<MaintenanceTicketDto>> UpdateTicketStatusAsync(UpdateTicketStatusDto input);

        // Invoices with pagination
        Task<MessageResponse<PaginatedResult<InvoiceDto>>> QueryInvoicesByBranchAsync(int page, int pageSize, long? branchId = null);
        Task<MessageResponse<PaginatedResult<InvoiceDto>>> QueryInvoicesByDriverAsync(int page, int pageSize, long? driverId);
        Task<MessageResponse<PaginatedResult<InvoiceDto>>> QueryInvoicesByVehicleAsync(int page, int pageSize, long vehicleId);
        Task<InvoiceDto?> GetInvoiceByIdAsync(long invoiceId);
        Task<MessageResponse<InvoiceDto>> UpdateInvoiceStatusAsync(long invoiceId, InvoiceStatus newStatus);

        // Dropdowns
        Task<List<SelectListItem>> GetPartCategoriesAsync();
        Task<List<SelectListItem>> GetPartsByCategoryAsync(int categoryId);
        List<SelectListItem> GetPriorityTypeOptions();
    }
}
