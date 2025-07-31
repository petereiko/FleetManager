using FleetManager.Business.Database.Entities;
using FleetManager.Business.DataObjects.Schedule;
using FleetManager.Business.Interfaces.ScheduleModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.ScheduleModule
{
    public class PublicHolidayService : IPublicHolidayService
    {
        private readonly FleetManagerDbContext _db;
        private readonly HttpClient _http;
        private readonly ILogger<PublicHolidayService> _log;

        public PublicHolidayService(FleetManagerDbContext db, IHttpClientFactory httpFactory, ILogger<PublicHolidayService> log)
        {
            _db = db;
            _http = httpFactory.CreateClient();
            _log = log;
        }

        public async Task<List<PublicHolidayDto>> GetHolidaysAsync(string countryCode, int year)
        {
            var start = new DateTime(year, 1, 1);
            var end = new DateTime(year, 12, 31);
            return await _db.PublicHolidays
                .Where(h => h.CountryCode == countryCode && h.Date >= start && h.Date <= end)
                .OrderBy(h => h.Date)
                .Select(h => new PublicHolidayDto
                {
                    LocalName = h.LocalName,
                    CountryCode = countryCode,
                    Date = h.Date,
                    CreatedDate = DateTime.UtcNow
                })
                .ToListAsync();
        }



        public async Task FetchAndStoreHolidaysAsync(string countryCode, int year)
        {
            // 1) fetch from the API
            var url = $"https://date.nager.at/api/v3/PublicHolidays/{year}/{countryCode}";
            List<NagerHolidayApiDto> holidays;
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                holidays = JsonConvert.DeserializeObject<List<NagerHolidayApiDto>>(json)!
                                     .ToList();
            }

            // 2) if we got any, map to your EF entity
            if (holidays.Count > 0)
            {
                var entities = holidays
                    // avoid duplicates by only inserting ones not already in the DB
                    .Where(h => !_db.PublicHolidays
                                     .Any(e => e.CountryCode == countryCode && e.Date == h.Date))
                    .Select(h => new PublicHoliday
                    {
                        CountryCode = h.CountryCode,
                        Date = h.Date,
                        LocalName = h.LocalName,
                        // Name = h.Name, // if you want the English name as well
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = "System"
                    })
                    .ToList();

                if (entities.Count > 0)
                {
                    await _db.PublicHolidays.AddRangeAsync(entities);
                    await _db.SaveChangesAsync();
                }
            }
        }

    }
}
