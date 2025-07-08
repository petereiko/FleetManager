using FleetManager.Business.DataObjects.VendorDto;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.RentalModule
{
    public interface IRentalService
    {
        Task<MessageResponse> ApplyForRentalAsync(VehicleRentalOnboardingDto dto);
        Task<MessageResponse<VehicleRentalDto>> UpdateRentalAsync(VehicleRentalDto dto);
        Task<MessageResponse> DeleteRentalAsync(long id);
        Task<VehicleRentalDto?> GetRentalByIdAsync(long id);
        Task<List<VehicleRentalDto>> GetRentalsForBranchAsync(long? branchId = null);
        Task<VehicleRentalAgreementDto?> GetRentalForAgreementAsync(long id);
        Task<VehicleRentalApplyViewModel> GetRentalApplyViewModelAsync(long vendorId, long companyBranchId);
        List<SelectListItem> GetRentalStatusOptions();
    }
}
