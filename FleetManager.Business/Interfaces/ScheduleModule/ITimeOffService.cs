using FleetManager.Business.DataObjects.Schedule;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.ScheduleModule
{
    public interface ITimeOffService
    {
        Task<MessageResponse<TimeOffRequestDto>> CreateRequestAsync(TimeOffRequestDto dto);
        Task<IEnumerable<TimeOffRequestDto>> GetRequestsByDriverAsync(long driverId);
        Task<IEnumerable<TimeOffRequestDto>> GetAllPendingRequestsAsync(long? branchId);
        Task<TimeOffRequestDto?> GetRequestByIdAsync(long requestId);
        Task ApproveRequestAsync(long requestId, string? adminComment = null);
        Task DenyRequestAsync(long requestId, string? adminComment = null);
        Task<List<SelectListItem>> GetCategoriesAsync();
    }
}
