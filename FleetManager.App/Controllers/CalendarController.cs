using FleetManager.Business.DataObjects.Schedule;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.DriverVehicleModule;
using FleetManager.Business.Interfaces.ScheduleModule;
using FleetManager.Business.Interfaces.UserModule;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CalendarController : ControllerBase
    {
        private readonly IPublicHolidayService _hol;
        private readonly ITimeOffService _to;
        private readonly IDriverVehicleService _assignmentService;
        private readonly IAuthUser _authUser;

        public CalendarController(IPublicHolidayService hol, ITimeOffService to, IAuthUser authUser, IDriverVehicleService assignmentService)
        {
            _hol = hol;
            _to = to;
            _authUser = authUser;
            _assignmentService = assignmentService;
        }

        // Called by your JS calendar to fetch events
        [HttpGet("events")]
        public async Task<IActionResult> Events(int year)
        {
            var country = "NG"; // or from config/authUser
            var holidays = await _hol.GetHolidaysAsync(country, year);
            var driverId = await _assignmentService.GetDriverIdByUserAsync(_authUser.UserId!);
            var offs = await _to.GetRequestsByDriverAsync(driverId);

            var evts = holidays
            .Select(h => new CalendarEventDto
            {
                Title = h.LocalName,
                Start = h.Date.ToString("yyyy‑MM‑dd"),
                Color = "red"
            })
            .Concat(offs.Select(o => new CalendarEventDto
            {
                Title = $"Time Off: {o.Reason}",
                Start = o.StartDate.ToString("yyyy‑MM‑dd"),
                End = o.EndDate.AddDays(1).ToString("yyyy‑MM‑dd"),
                Color = o.Status switch
                {
                    TimeOffStatus.Pending => "orange",
                    TimeOffStatus.Approved => "green",
                    TimeOffStatus.Denied => "gray",
                    _ => "blue"
                }
            }))
            .ToList();

            return Ok(evts);
        }
    }
    
}
