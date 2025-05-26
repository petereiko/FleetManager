using FleetManager.Business.DataObjects;
using FleetManager.Business.UtilityModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.DriverVehicleModule
{
    public interface IDriverVehicleService
    {
        Task<MessageResponse<DriverVehicleDto>> AssignVehicleAsync(DriverVehicleDto dto, string createdBy);
        Task<MessageResponse<DriverVehicleDto>> UpdateAssignmentAsync(DriverVehicleDto dto, string modifiedBy);
        Task<MessageResponse> UnassignVehicleAsync(long assignmentId);

        /// <summary>
        /// Query all current and past assignments for a given driver.
        /// </summary>
        IQueryable<DriverVehicleListItemDto> QueryAssignmentsByDriver(long driverId);

        /// <summary>
        /// Query all assignments for a given vehicle.
        /// </summary>
        IQueryable<DriverVehicleListItemDto> QueryAssignmentsByVehicle(long vehicleId);
    }
}
