using FleetManager.Business.Database.Entities;
using FleetManager.Business.DataObjects.Schedule;
using FleetManager.Business.Interfaces.ScheduleModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

        public async Task FetchAndStoreHolidaysAsync(string countryCode, int year)
        {
            var url = $"https://date.nager.at/api/v3/PublicHolidays/{year}/{countryCode}";
            var resp = await _http.GetFromJsonAsync<List<NagerHolidayApiDto>>(url);
            if (resp == null) throw new Exception("Failed to fetch holidays");

            foreach (var h in resp)
            {
                if (!await _db.PublicHolidays
                       .AnyAsync(x => x.CountryCode == countryCode && x.Date == h.Date))
                {
                    _db.PublicHolidays.Add(new PublicHoliday
                    {
                        CountryCode = countryCode,
                        Date = h.Date,
                        LocalName = h.LocalName,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = "System",
                        IsActive = true

                    });
                }
            }
            await _db.SaveChangesAsync();
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

        private class NagerHolidayApiDto
        {
            public DateTime Date { get; set; }
            public string LocalName { get; set; } = null!;
            public string Name { get; set; } = null!;
            public string CountryCode { get; set; } = null!;
            public bool Fixed { get; set; }
        }
    }
}
