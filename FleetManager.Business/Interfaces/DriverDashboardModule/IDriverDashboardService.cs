using FleetManager.Business.DataObjects.DashboardDriverDto;
using FleetManager.Business.UtilityModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.DriverDashboardModule
{
    public interface IDriverDashboardService
    {
        Task<MessageResponse<DriverDashboardDto>> GetDriverDashboardAsync();
    }
}
