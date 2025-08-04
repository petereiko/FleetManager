using FleetManager.Business.Database.Entities;
using FleetManager.Business.DataObjects.Schedule;
using FleetManager.Business.Interfaces.ScheduleModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.UtilityModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.ScheduleModule
{
    public class TimeOffCategoryService : ITimeOffCategoryService
    {
        private readonly FleetManagerDbContext _db;
        private readonly ILogger<TimeOffCategoryService> _logger;
        private readonly IAuthUser _authUser;

        public TimeOffCategoryService(
            FleetManagerDbContext db,
            ILogger<TimeOffCategoryService> logger,
            IAuthUser authUser)
        {
            _db = db;
            _logger = logger;
            _authUser = authUser;
        }
        private void EnsureAdminOrOwner()
        {
            var roles = (_authUser.Roles ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim());
            if (!roles.Contains("Company Admin") &&
                !roles.Contains("Company Owner") &&
                !roles.Contains("Super Admin") &&
                !roles.Contains("Driver"))
            {
                throw new UnauthorizedAccessException("Insufficient permissions to access fuel logs.");
            }
        }

        public async Task<List<TimeOffCategoryDto>> GetAllAsync()
        {
            try
            {
                // grab the current user's branch (nullable)
                var branchId = _authUser.CompanyBranchId;

                // base query
                var q = _db.Set<TimeOffCategory>()
                    .AsNoTracking()
                    // include only system (null) or matching branch
                    .Where(c => c.CompanyBranchId == null
                             || (branchId.HasValue && c.CompanyBranchId == branchId.Value));

                return await q
                    .OrderBy(c => c.Name)
                    .Select(c => new TimeOffCategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        CreatedAt = c.CreatedDate,
                        CreatedBy = c.CreatedBy,
                        UpdatedAt = c.ModifiedDate,
                        UpdatedBy = c.ModifiedBy
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching time-off categories for branch {BranchId}", _authUser.CompanyBranchId);
                return new List<TimeOffCategoryDto>();
            }
        }

        public async Task<TimeOffCategoryDto?> GetByIdAsync(long id)
        {
            var branchId = _authUser.CompanyBranchId;

            var c = await _db.Set<TimeOffCategory>()
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.Id == id
                 && (c.CompanyBranchId == null
                     || (branchId.HasValue && c.CompanyBranchId == branchId.Value))
                );

            if (c == null) return null;

            return new TimeOffCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                CreatedAt = c.CreatedDate,
                CreatedBy = c.CreatedBy,
                UpdatedAt = c.ModifiedDate,
                UpdatedBy = c.ModifiedBy
            };
        }

        public async Task<MessageResponse<TimeOffCategoryDto>> CreateAsync(string name, string? description)
        {
            EnsureAdminOrOwner();
            var resp = new MessageResponse<TimeOffCategoryDto>();
            try
            {
                var exists = await _db.Set<TimeOffCategory>().AnyAsync(c => c.Name == name);
                if (exists)
                {
                    resp.Message = $"Category '{name}' already exists.";
                    return resp;
                }
                var branchId = _authUser.CompanyBranchId;

                var entity = new TimeOffCategory
                {
                    Name = name,
                    Description = description,
                    CreatedBy = _authUser.UserId,
                    CompanyBranchId = branchId,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true

                };
                _db.Set<TimeOffCategory>().Add(entity);
                await _db.SaveChangesAsync();

                resp.Success = true;
                resp.Result = new TimeOffCategoryDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Description = entity.Description,
                    CreatedAt = entity.CreatedDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create time‑off category");
                resp.Message = "An error occurred creating the category.";
            }
            return resp;
        }

        public async Task<MessageResponse<TimeOffCategoryDto>> UpdateAsync(long id, string name, string? description)
        {
            EnsureAdminOrOwner();
            var resp = new MessageResponse<TimeOffCategoryDto>();
            try
            {
                var branchId = _authUser.CompanyBranchId;
                var entity = await _db.Set<TimeOffCategory>()
                    .FirstOrDefaultAsync(c =>
                        c.Id == id
                     && branchId.HasValue
                     && c.CompanyBranchId == branchId.Value
                    );

                if (entity == null)
                {
                    resp.Message = "Category not found or you don’t have permission to edit it.";
                    return resp;
                }

                entity.Name = name;
                entity.Description = description;
                entity.ModifiedDate = DateTime.UtcNow;
                entity.ModifiedBy = _authUser.UserId; 

                await _db.SaveChangesAsync();

                resp.Success = true;
                resp.Result = new TimeOffCategoryDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Description = entity.Description,
                    CreatedAt = entity.CreatedDate,
                    CreatedBy = entity.CreatedBy,
                    UpdatedAt = entity.ModifiedDate,
                    UpdatedBy = entity.ModifiedBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update category {CategoryId}", id);
                resp.Message = "An error occurred updating the category.";
            }
            return resp;
        }
        public async Task<MessageResponse> DeleteAsync(long id)
        {
            var resp = new MessageResponse();
            try
            {
                var branchId = _authUser.CompanyBranchId;
                var entity = await _db.Set<TimeOffCategory>()
                    .FirstOrDefaultAsync(c => c.Id == id
                     && (branchId.HasValue
                         && c.CompanyBranchId == branchId.Value)
                    );


                if (entity == null)
                {
                    resp.Message = "Category not found or you don’t have permission to delete it.";
                    return resp;
                }

                _db.Set<TimeOffCategory>().Remove(entity);
                await _db.SaveChangesAsync();

                resp.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete category {CategoryId}", id);
                resp.Message = "An error occurred deleting the category.";
            }
            return resp;
        }
    }
}
