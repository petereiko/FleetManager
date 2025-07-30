using FleetManager.Business.DataObjects.Schedule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.ScheduleModule
{
    public interface IPublicHolidayService
    {
        Task FetchAndStoreHolidaysAsync(string countryCode, int year);
        Task<List<PublicHolidayDto>> GetHolidaysAsync(string countryCode, int year);
    }

}
