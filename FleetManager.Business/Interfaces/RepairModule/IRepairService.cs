using FleetManager.Business.DataObjects.RepairDto;
using FleetManager.Business.DataObjects.RepairHistoryDto;
using FleetManager.Business.Enums;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.RepairModule
{
    public interface IRepairService
    {
        Task<MessageResponse<PaginatedResult<RepairDto>>> QueryRepairsByBranchAsync(int page, int pageSize, long? branchId = null);
        Task<MessageResponse<PaginatedResult<RepairDto>>> QueryRepairsByVehicleAsync(int page, int pageSize, long vehicleId);
        Task<RepairDto?> GetRepairByIdAsync(long repairId);
        Task<MessageResponse<RepairDto>> CreateRepairAsync(RepairInputDto input, string createdByUserId);
        Task<MessageResponse<RepairDto>> UpdateRepairAsync(UpdateRepairInputDto input);
        Task<MessageResponse<RepairDto>> UpdateRepairStatusAsync(UpdateRepairStatusDto input);

        // invoice helpers
        Task<MessageResponse<PaginatedResult<RepairInvoiceDto>>> QueryRepairInvoicesByBranchAsync(int page, int pageSize, long? branchId = null);
        Task<RepairInvoiceDto?> GetRepairInvoiceByIdAsync(long invoiceId);
        Task<MessageResponse<RepairInvoiceDto>> UpdateRepairInvoiceStatusAsync(long invoiceId, InvoiceStatus newStatus);

        // Dropdowns
        Task<List<SelectListItem>> GetPartCategoriesAsync();
        Task<List<SelectListItem>> GetPartsByCategoryAsync(int categoryId);
        List<SelectListItem> GetPriorityTypeOptions();
    }

}
