using FleetManager.Business.Database.Entities;
using FleetManager.Business.DataObjects;
using FleetManager.Business.Interfaces.ContactDirectoryModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace FleetManager.Business.Implementations.ContactDirectoryModule
{
    public class ContactDirectoryService : IContactDirectoryService
    {
        private readonly FleetManagerDbContext _context;
        private readonly IAuthUser _authUser;
        private readonly ILogger<ContactDirectoryService> _logger;

        public ContactDirectoryService(FleetManagerDbContext context, IAuthUser authUser, ILogger<ContactDirectoryService> logger)
        {
            _context = context;
            _authUser = authUser;
            _logger = logger;
        }

        private void EnsureAdminOrOwner()
        {
            var roles = (_authUser.Roles ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim());

            if (!roles.Contains("Company Admin")
             && !roles.Contains("Company Owner")
             && !roles.Contains("Super Admin"))
            {
                throw new UnauthorizedAccessException("Insufficient permissions to manage contacts.");
            }
        }


        public async Task<MessageResponse> AddContactAsync(ContactDirectoryDto dto)
        {
            EnsureAdminOrOwner();
            var resp = new MessageResponse();
            try
            {
                var contact = new ContactDirectory
                {
                    CompanyBranchId=_authUser.CompanyBranchId,
                    ContactName = dto.ContactName,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    Address = dto.Address,
                    VendorName = dto.VendorName,
                    CategoryId = dto.CategoryId,
                    Services = dto.Services,
                    IsActive = true,
                    CreatedBy = _authUser.UserId,
                    CreatedDate = DateTime.UtcNow
                };

                _context.ContactDirectories.Add(contact);
                await _context.SaveChangesAsync();

                resp.Success = true;
                resp.Message = "Contact added successfully.";
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddContactAsync failed");
                resp.Message = "An error occurred while adding the contact.";
                return resp;
            }
        }

        public async Task<MessageResponse<ContactDirectoryDto>> UpdateContactAsync(ContactDirectoryDto dto)
        {
            EnsureAdminOrOwner();
            var resp = new MessageResponse<ContactDirectoryDto>();
            try
            {
                var entity = await _context.ContactDirectories.FindAsync(dto.Id);
                if (entity == null)
                {
                    resp.Message = "Contact not found.";
                    return resp;
                }

                entity.ContactName = dto.ContactName;
                entity.Email = dto.Email;
                entity.PhoneNumber = dto.PhoneNumber;
                entity.Address = dto.Address;
                entity.CompanyBranchId = _authUser.CompanyBranchId;
                entity.VendorName = dto.VendorName;
                entity.CategoryId = dto.CategoryId;
                entity.Services = dto.Services;
                entity.IsActive = dto.IsActive;
                entity.ModifiedBy = _authUser.UserId;
                entity.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                resp.Success = true;
                resp.Result = dto;
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateContactAsync failed");
                resp.Message = "Error updating contact.";
                return resp;
            }
        }

        public async Task<MessageResponse> DeleteContactAsync(long id)
        {
            EnsureAdminOrOwner();
            var resp = new MessageResponse();
            try
            {
                var contact = await _context.ContactDirectories.FindAsync(id);
                if (contact == null)
                {
                    resp.Message = "Contact not found.";
                    return resp;
                }

                _context.ContactDirectories.Remove(contact);
                await _context.SaveChangesAsync();

                resp.Success = true;
                resp.Message = "Contact deleted.";
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteContactAsync failed");
                resp.Message = "Failed to delete contact.";
                return resp;
            }
        }

        public async Task<ContactDirectoryDto?> GetContactByIdAsync(long id)
        {
            EnsureAdminOrOwner();

            var contact = await _context.ContactDirectories
                        .AsNoTracking()
                        .Include(c => c.Category)
                        .FirstOrDefaultAsync(c => c.Id == id);

            if (contact == null) return null;

            var stats = await GetRatingStatsAsync(id);
            var avg = stats?.AverageRating ?? 0;
            var count = stats?.RatingCount ?? 0;

            return new ContactDirectoryDto
            {
                Id = contact.Id,
                ContactName = contact.ContactName,
                Email = contact.Email,
                PhoneNumber = contact.PhoneNumber,
                Address = contact.Address,
                VendorName = contact.VendorName ?? "Couldn't retrieve name",
                CategoryId = contact.CategoryId,
                CategoryName = contact.Category?.Name,
                Services = contact.Services,
                IsActive = contact.IsActive,
                CreatedDate = contact.CreatedDate,
                AverageRating = Math.Round(avg, 2),
                RatingCount = count
            };
        }

        public async Task<List<ContactDirectoryDto>> GetAllContactsAsync()
        {
            EnsureAdminOrOwner();
            var bId = _authUser.CompanyBranchId ?? throw new InvalidOperationException("BranchId missing");

            var contacts = await _context.ContactDirectories
                                .AsNoTracking()
                                .Include(c => c.Category)
                                .Where(c => c.CompanyBranchId == bId)
                                .OrderByDescending(c => c.CreatedDate)
                                .ToListAsync();

            // get ratings grouped
            var contactIds = contacts.Select(c => c.Id).ToList();

            var ratingGroups = await _context.Set<ContactRating>()
                                  .Where(r => contactIds.Contains(r.ContactDirectoryId))
                                  .GroupBy(r => r.ContactDirectoryId)
                                  .Select(g => new { ContactId = g.Key, Avg = g.Average(x => x.Rating), Count = g.Count() })
                                  .ToListAsync();

            var ratingDict = ratingGroups.ToDictionary(x => x.ContactId, x => (avg: x.Avg, count: x.Count));

            return contacts.Select(c =>
            {
                ratingDict.TryGetValue(c.Id, out var stats);
                var avg = stats == default ? 0 : stats.avg;
                var count = stats == default ? 0 : stats.count;

                return new ContactDirectoryDto
                {
                    Id = c.Id,
                    ContactName = c.ContactName,
                    Email = c.Email,
                    PhoneNumber = c.PhoneNumber,
                    Address = c.Address,
                    VendorName = c.VendorName,
                    CategoryId = c.CategoryId,
                    CategoryName = c.Category?.Name,
                    Services = c.Services,
                    IsActive = c.IsActive,
                    CreatedDate = c.CreatedDate,
                    AverageRating = Math.Round(avg, 2),
                    RatingCount = count
                };
            }).ToList();
        }

        public async Task<MessageResponse<ContactRatingResultDto>> AddOrUpdateRatingAsync(ContactRatingDto dto)
        {
            // allow any authenticated user to rate; if you want to restrict, use EnsureAdminOrOwner()
            var resp = new MessageResponse<ContactRatingResultDto>();

            if (string.IsNullOrWhiteSpace(_authUser.UserId))
            {
                resp.Message = "User must be authenticated to rate.";
                return resp;
            }

            if (dto.Rating < 1 || dto.Rating > 5)
            {
                resp.Message = "Rating must be between 1 and 5.";
                return resp;
            }

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // ensure contact exists and belongs to the user's branch if needed
                var contact = await _context.ContactDirectories
                                    .FirstOrDefaultAsync(c => c.Id == dto.ContactId);
                if (contact == null)
                {
                    resp.Message = "Contact not found.";
                    return resp;
                }

                // optional: ensure same branch or same company, depending on rules
                // if (contact.CompanyBranchId != _authUser.CompanyBranchId) { ... }

                var existing = await _context.Set<ContactRating>()
                                    .SingleOrDefaultAsync(r => r.ContactDirectoryId == dto.ContactId
                                                               && r.UserId == _authUser.UserId);

                if (existing != null)
                {
                    existing.Rating = dto.Rating;
                    existing.Comment = dto.Comment;
                    existing.CompanyBranchId = _authUser.CompanyBranchId;
                    existing.ModifiedBy = _authUser.UserId;
                    existing.ModifiedDate = DateTime.UtcNow;
                }
                else
                {
                    var newRating = new ContactRating
                    {
                        ContactDirectoryId = dto.ContactId,
                        Rating = dto.Rating,
                        Comment = dto.Comment,
                        CompanyBranchId = _authUser.CompanyBranchId,
                        UserId = _authUser.UserId!,
                        UserDisplayName = _authUser.FullName, // if you have it
                        CreatedBy = _authUser.UserId,
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.Set<ContactRating>().Add(newRating);
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                // compute updated stats (single query)
                var stats = await _context.Set<ContactRating>()
                             .Where(r => r.ContactDirectoryId == dto.ContactId)
                             .GroupBy(r => r.ContactDirectoryId)
                             .Select(g => new
                             {
                                 Avg = g.Average(x => x.Rating),
                                 Count = g.Count()
                             }).FirstOrDefaultAsync();

                resp.Success = true;
                resp.Result = new ContactRatingResultDto
                {
                    ContactId = dto.ContactId,
                    AverageRating = stats?.Avg ?? 0,
                    RatingCount = stats?.Count ?? 0
                };

                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddOrUpdateRatingAsync failed");
                resp.Message = "Failed to save rating.";
                return resp;
            }
        }

        public async Task<MessageResponse<ContactRatingResultDto>> RemoveMyRatingAsync(long contactId)
        {
            var resp = new MessageResponse<ContactRatingResultDto>();
            if (string.IsNullOrWhiteSpace(_authUser.UserId))
            {
                resp.Message = "User must be authenticated.";
                return resp;
            }

            try
            {
                var existing = await _context.Set<ContactRating>()
                    .SingleOrDefaultAsync(r => r.ContactDirectoryId == contactId && r.UserId == _authUser.UserId);

                if (existing == null)
                {
                    resp.Message = "No rating to remove.";
                    return resp;
                }

                _context.Set<ContactRating>().Remove(existing);
                await _context.SaveChangesAsync();

                var stats = await _context.Set<ContactRating>()
                             .Where(r => r.ContactDirectoryId == contactId)
                             .GroupBy(r => r.ContactDirectoryId)
                             .Select(g => new { Avg = g.Average(x => x.Rating), Count = g.Count() })
                             .FirstOrDefaultAsync();

                resp.Success = true;
                resp.Result = new ContactRatingResultDto
                {
                    ContactId = contactId,
                    AverageRating = stats?.Avg ?? 0,
                    RatingCount = stats?.Count ?? 0
                };

                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RemoveMyRatingAsync failed");
                resp.Message = "Failed to remove rating.";
                return resp;
            }
        }



        public async Task<ContactRatingResultDto> GetRatingStatsAsync(long contactId)
        {
            var groups = await _context.Set<ContactRating>()
                           .Where(r => r.ContactDirectoryId == contactId)
                           .GroupBy(r => r.Rating)
                           .Select(g => new { Rating = g.Key, Count = g.Count() })
                           .ToListAsync();

            var total = groups.Sum(g => g.Count);
            var weighted = groups.Sum(g => g.Rating * g.Count);
            var average = total == 0 ? 0 : (double)weighted / total;

            var dist = groups.ToDictionary(g => g.Rating, g => g.Count);
            for (int i = 1; i <= 5; i++) if (!dist.ContainsKey(i)) dist[i] = 0;

            return new ContactRatingResultDto
            {
                ContactId = contactId,
                AverageRating = Math.Round(average, 2),
                RatingCount = total,
                RatingDistribution = dist
            };
        }



        //public async Task<(double avg, int count, Dictionary<int, int> distribution)> GetRatingStatsAsync(long contactId)
        //{
        //    // distribution and average in one pass
        //    var ratings = await _context.Set<ContactRating>()
        //                        .Where(r => r.ContactDirectoryId == contactId)
        //                        .GroupBy(r => r.Rating)
        //                        .Select(g => new { Rating = g.Key, Count = g.Count() })
        //                        .ToListAsync();

        //    var total = ratings.Sum(r => r.Count);
        //    var weightedSum = ratings.Sum(r => r.Rating * r.Count);
        //    var avg = total == 0 ? 0 : (double)weightedSum / total;

        //    var distribution = ratings.ToDictionary(x => x.Rating, x => x.Count);
        //    // ensure keys 1..5 exist
        //    for (int i = 1; i <= 5; i++) if (!distribution.ContainsKey(i)) distribution[i] = 0;

        //    return (avg, total, distribution);
        //}



        public List<SelectListItem> GetCategoryOptions()
            => _context.VendorCategories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();
    }

}
