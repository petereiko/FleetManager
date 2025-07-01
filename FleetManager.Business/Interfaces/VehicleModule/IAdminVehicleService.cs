using FleetManager.Business.DataObjects.VehicleDto;
using FleetManager.Business.Enums;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.VehicleModule
{
    public interface IAdminVehicleService
    {
        // CRUD
        Task<MessageResponse<VehicleDto>> CreateVehicleAsync(VehicleDto dto, string createdByUserId);
        Task<MessageResponse<VehicleDto>> UpdateVehicleAsync(VehicleDto dto, string modifiedByUserId);
        Task<MessageResponse> DeleteVehicleAsync(long id);
        //Task<MessageResponse> DeleteVehiclePhotoAsync(long photoId);
        Task<MessageResponse> DeleteVehicleDocumentAsync(long documentId);

        Task<MessageResponse> UpdateVehicleStatusAsync(long vehicleId, VehicleStatus newStatus, string modifiedBy);

        //Task LoadMakes();
        //Task LoadModels();

        // Retrieval
        Task<VehicleDto> GetVehicleByIdAsync(long id);
        Task<List<VehicleListItemDto>> GetVehiclesAsync(VehicleFilterDto filter);

        // Lookups for dropdowns
        List<SelectListItem> GetFuelTypeOptions();
        List<SelectListItem> GetTransmissionTypeOptions();
        List<SelectListItem> GetStatusOptions();
        List<SelectListItem> GetVehicleTypeOptions();
        Task<List<SelectListItem>> GetBranchOptionsAsync(long companyId);

        List<SelectListItem> GetVehicleMakes();
        Task<List<SelectListItem>> GetVehicleModelsByMakeId(int makeId);




        /// <summary>
        /// Returns a queryable sequence of VehicleListItemDto for further paging.
        /// </summary>
        IQueryable<VehicleListItemDto> QueryVehicles(VehicleFilterDto filter);

    }
}
