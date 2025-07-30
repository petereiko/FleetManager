using FleetManager.Business.Database.Entities;
using FleetManager.Business.DataObjects.Schedule;
using FleetManager.Business.Interfaces.ScheduleModule;
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

        public TimeOffCategoryService(
            FleetManagerDbContext db,
            ILogger<TimeOffCategoryService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<TimeOffCategoryDto>> GetAllAsync()
            => await _db.Set<TimeOffCategory>()
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new TimeOffCategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description
                })
                .ToListAsync();

        public async Task<TimeOffCategoryDto?> GetByIdAsync(long id)
        {
            var c = await _db.Set<TimeOffCategory>()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
            return c == null ? null : new TimeOffCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            };
        }

        public async Task<MessageResponse<TimeOffCategoryDto>> CreateAsync(string name, string? description)
        {
            var resp = new MessageResponse<TimeOffCategoryDto>();
            try
            {
                var exists = await _db.Set<TimeOffCategory>().AnyAsync(c => c.Name == name);
                if (exists)
                {
                    resp.Message = $"Category '{name}' already exists.";
                    return resp;
                }

                var entity = new TimeOffCategory
                {
                    Name = name,
                    Description = description,
                    CreatedBy = "System",
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
            var resp = new MessageResponse<TimeOffCategoryDto>();
            try
            {
                var entity = await _db.Set<TimeOffCategory>().FindAsync(id);
                if (entity == null)
                {
                    resp.Message = "Category not found.";
                    return resp;
                }

                entity.Name = name;
                entity.Description = description;
                await _db.SaveChangesAsync();

                resp.Success = true;
                resp.Result = new TimeOffCategoryDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Description = entity.Description,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "System"

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
                var entity = await _db.Set<TimeOffCategory>().FindAsync(id);
                if (entity == null)
                {
                    resp.Message = "Category not found.";
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
