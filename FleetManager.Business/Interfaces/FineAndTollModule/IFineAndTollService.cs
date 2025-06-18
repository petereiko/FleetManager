using FleetManager.Business.DataObjects;
using FleetManager.Business.Enums;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.FineAndTollModule
{
    public interface IFineAndTollService
    {
        IQueryable<FineAndTollDto> QueryByDriver(string driverUserId);
        IQueryable<FineAndTollDto> QueryByBranch(long? branchId = null);
        Task<FineAndTollDto?> GetByIdAsync(long id);
        Task<MessageResponse<FineAndTollDto>> CreateAsync(FineAndTollInputDto input, string createdByUserId);
        Task<MessageResponse<FineAndTollDto>> UpdateStatusAsync(long id, FineTollStatus newStatus, string modifiedByUserId);
        //Task<MessageResponse> DeleteAsync(long id);
        List<SelectListItem> GetFineStatusOptions();
        List<SelectListItem> GetFineTollTypeOptions();
    }
}
