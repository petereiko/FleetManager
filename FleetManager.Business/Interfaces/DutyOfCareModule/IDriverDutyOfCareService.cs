using FleetManager.Business.DataObjects;
using FleetManager.Business.UtilityModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.DutyOfCareModule
{
    public interface IDriverDutyOfCareService
    {
       
        IQueryable<DriverDutyOfCareDto> QueryByDriver(long driverId);
        IQueryable<DriverDutyOfCareDto> QueryByVehicle(long vehicleId);
        Task<DriverDutyOfCareDto?> GetByIdAsync(long id);
        Task<MessageResponse<DriverDutyOfCareDto>> CreateAsync(DriverDutyOfCareDto dto, string createdBy);
        Task<MessageResponse<DriverDutyOfCareDto>> UpdateAsync(DriverDutyOfCareDto dto, string modifiedBy);
        Task<MessageResponse> DeleteAsync(long id);
        IQueryable<DriverDutyOfCareDto> QueryAll();
        //Task<DriverDutyOfCareDto> GetDutyOfCareByIdAsync(long id);

    }
}
