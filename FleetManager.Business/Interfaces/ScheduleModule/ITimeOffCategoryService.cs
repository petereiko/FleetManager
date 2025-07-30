using FleetManager.Business.DataObjects.Schedule;
using FleetManager.Business.UtilityModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.ScheduleModule
{
    public interface ITimeOffCategoryService
    {
        Task<List<TimeOffCategoryDto>> GetAllAsync();
        Task<TimeOffCategoryDto?> GetByIdAsync(long id);
        Task<MessageResponse<TimeOffCategoryDto>> CreateAsync(string name, string? description);
        Task<MessageResponse<TimeOffCategoryDto>> UpdateAsync(long id, string name, string? description);
        Task<MessageResponse> DeleteAsync(long id);
    }
}
