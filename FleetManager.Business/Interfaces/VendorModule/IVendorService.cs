using FleetManager.Business.DataObjects;
using FleetManager.Business.DataObjects.VendorDto;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.VendorModule
{
    public interface IVendorService
    {
        Task<MessageResponse> OnboardVendorAsync(VendorOnboardingDto dto);
        Task<MessageResponse<VendorDto>> UpdateVendorAsync(VendorDto dto);
        Task<VendorDashboardViewModel?> GetVendorDashboardAsync();
        Task<MessageResponse> DeleteVendorAsync(long id);
        Task<VendorDto?> GetVendorByIdAsync(long id);
        //Task<List<VendorDto>> GetVendorsAsync();
        Task<List<VendorDto>> GetVendorsAsync(string? search, int? categoryId = null);
        List<SelectListItem> GetVendorCategoryOptions();
        Task<List<StateDto>> GetAllStatesAsync();
        //List<SelectListItem> GetVendorServiceOptions();
    }
}
