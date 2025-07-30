using FleetManager.Business.Database.Entities;
using FleetManager.Business.DataObjects.Schedule;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.ScheduleModule;
using FleetManager.Business.Interfaces.UserModule;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.ScheduleModule
{
    public class TimeOffService : ITimeOffService
    {
        private readonly FleetManagerDbContext _db;
        private readonly IAuthUser _auth;
        private readonly ILogger<TimeOffService> _log;

        public TimeOffService(FleetManagerDbContext db, IAuthUser auth, ILogger<TimeOffService> log)
        {
            _db = db;
            _auth = auth;
            _log = log;
        }

        public async Task<TimeOffRequestDto> CreateRequestAsync(TimeOffRequestDto dto)
        {
            var entity = new TimeOffRequest
            {
                DriverId = dto.DriverId,
                CompanyBranchId = dto.CompanyBranchId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Reason = dto.Reason,
                Status = TimeOffStatus.Pending,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                CategoryId = dto.CategoryId,
                CreatedBy = _auth.UserId
            };

            _db.TimeOffRequests.Add(entity);
            await _db.SaveChangesAsync();

            dto.Id = entity.Id;
            return dto;
        }

        public async Task<TimeOffRequestDto?> GetRequestByIdAsync(long requestId)
        {
            // fetch the single request including driver, category, and reviewer name
            var r = await _db.TimeOffRequests
                .AsNoTracking()
                .Where(rq => rq.Id == requestId && rq.CompanyBranchId == _auth.CompanyBranchId)
                .Include(rq => rq.Driver).ThenInclude(d => d.User)
                .Include(rq => rq.Category)
                .FirstOrDefaultAsync();

            if (r == null) return null;

            // find reviewer admin name if present
            string? reviewerName = null;
            if (r.ReviewedBy != null)
            {
                var admin = await _db.CompanyAdmins
                    .AsNoTracking()
                    .Where(ca => ca.UserId == r.ReviewedBy)
                    .Select(ca => ca.User)
                    .FirstOrDefaultAsync();

                if (admin != null)
                    reviewerName = $"{admin.FirstName} {admin.LastName}";
            }

            return new TimeOffRequestDto
            {
                Id = r.Id,
                CompanyBranchId = r.CompanyBranchId,
                DriverId = r.DriverId,
                RequestedBy = $"{r.Driver.User.FirstName} {r.Driver.User.LastName}",
                RequestedAt = r.CreatedDate,
                CategoryId = r.CategoryId,
                CategoryName = r.Category.Name,
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                Reason = r.Reason,
                Status = r.Status,
                AdminNotes = r.AdminNotes,
                ReviewedAt = r.ReviewedAt,
                ReviewedByName = reviewerName
            };
        }

        public async Task<IEnumerable<TimeOffRequestDto>> GetRequestsByDriverAsync(long driverId)
        {
            // Branch filter too—driver only sees their own branch
            var branchId = _auth.CompanyBranchId;
            var q = from r in _db.TimeOffRequests.AsNoTracking()
                    where r.DriverId == driverId
                       && r.CompanyBranchId == branchId
                    join d in _db.Drivers.AsNoTracking() on r.DriverId equals d.Id
                    join u in _db.Users.AsNoTracking() on d.UserId equals u.Id
                    join cat in _db.TimeOffCategories.AsNoTracking() on r.CategoryId equals cat.Id
                    from ca in _db.CompanyAdmins.AsNoTracking()
                        .Where(ca => ca.UserId == r.ReviewedBy).DefaultIfEmpty()
                    from au in _db.Users.AsNoTracking()
                        .Where(au => au.Id == ca.UserId).DefaultIfEmpty()
                    select new TimeOffRequestDto
                    {
                        Id = r.Id,
                        CompanyBranchId = r.CompanyBranchId,
                        DriverId = r.DriverId,
                        CategoryId = r.CategoryId,
                        CategoryName = cat.Name,
                        StartDate = r.StartDate,
                        EndDate = r.EndDate,
                        Reason = r.Reason,
                        Status = r.Status,
                        AdminNotes = r.AdminNotes,
                        RequestedBy = $"{u.FirstName} {u.LastName}",
                        RequestedAt = r.CreatedDate,
                        ReviewedAt = r.ReviewedAt,
                        ReviewedByName = au != null
                                         ? $"{au.FirstName} {au.LastName}"
                                         : null
                    };

            return await q
                .OrderByDescending(r => r.StartDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<TimeOffRequestDto>> GetAllPendingRequestsAsync(long? branchId = null)
        {
            var b = branchId ?? _auth.CompanyBranchId;

            var q = from r in _db.TimeOffRequests.AsNoTracking()
                    where r.Status == TimeOffStatus.Pending
                       && r.CompanyBranchId == b
                    join d in _db.Drivers.AsNoTracking() on r.DriverId equals d.Id
                    join u in _db.Users.AsNoTracking() on d.UserId equals u.Id
                    join cat in _db.TimeOffCategories.AsNoTracking() on r.CategoryId equals cat.Id
                    select new TimeOffRequestDto
                    {
                        Id = r.Id,
                        CompanyBranchId = r.CompanyBranchId,
                        DriverId = r.DriverId,
                        CategoryId = r.CategoryId,
                        CategoryName = cat.Name,
                        StartDate = r.StartDate,
                        EndDate = r.EndDate,
                        Reason = r.Reason,
                        Status = r.Status,
                        AdminNotes = r.AdminNotes,
                        RequestedBy = $"{u.FirstName} {u.LastName}",
                        RequestedAt = r.CreatedDate
                        // leave Reviewed fields null until approved/denied
                    };

            return await q
                .OrderByDescending(r => r.StartDate)
                .ToListAsync();
        }

        public async Task ApproveRequestAsync(long requestId, string? adminComment = null)
        {
            var r = await _db.TimeOffRequests.FindAsync(requestId)
                  ?? throw new KeyNotFoundException("Time‑off request not found.");

            r.Status = TimeOffStatus.Approved;
            r.AdminNotes = adminComment;
            r.ReviewedAt = DateTime.UtcNow;
            r.ReviewedBy = _auth.UserId;
            await _db.SaveChangesAsync();
        }

        public async Task DenyRequestAsync(long requestId, string? adminComment = null)
        {
            var r = await _db.TimeOffRequests.FindAsync(requestId)
                  ?? throw new KeyNotFoundException("Time‑off request not found.");

            r.Status = TimeOffStatus.Denied;
            r.AdminNotes = adminComment;
            r.ReviewedAt = DateTime.UtcNow;
            r.ReviewedBy = _auth.UserId;
            await _db.SaveChangesAsync();
        }


        public async Task<List<SelectListItem>> GetCategoriesAsync()
        {
            return await _db.TimeOffCategories
                .AsNoTracking()
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();
        }


    }
}
