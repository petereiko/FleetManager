using FleetManager.Business.DataObjects;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.ManageDriverModule
{
    public interface IManageDriverService
    {
        Task<MessageResponse> OnboardDriverAsync(DriverOnboardingDto dto, string createdByUserId);
        Task<MessageResponse<DriverDto>> UpdateDriverAsync(DriverDto dto, string modifiedByUserId);
        Task<MessageResponse> DeleteDriverAsync(long id);
        Task<DriverDto?> GetDriverByIdAsync(long id);
        Task<List<DriverDto>> GetDriversForBranchAsync(long? branchId = null);
        IQueryable<DriverListItemDto> QueryDriversForBranch(long? branchId);
        List<SelectListItem> GetGenderOptions();
        List<SelectListItem> GetEmploymentStatusOptions();
        List<SelectListItem> GetShiftStatusOptions();
        List<SelectListItem> GetLicenseCategoryOptions();

    }
}
