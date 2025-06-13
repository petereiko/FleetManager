using FleetManager.Business.DataObjects;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.FuelLogModule
{
    public interface IFuelLogService
    {
        IQueryable<FuelLogDto> QueryByBranch(long? branchId = null);
        IQueryable<FuelLogDto> QueryByDriver(long driverId);
        Task<MessageResponse<FuelLogDto>> CreateAsync(FuelLogInputDto input, string createdByUserId);
        Task<MessageResponse<FuelLogDto>> UpdateAsync(long id, FuelLogInputDto input, string modifiedByUserId);
        Task<MessageResponse> DeleteAsync(long id);
        Task<FuelLogDto?> GetByIdAsync(long id);
        List<SelectListItem> GetFuelTypeOptions();
    }

}
