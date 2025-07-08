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
                CreatedDate = contact.CreatedDate
            };
        }

        public async Task<List<ContactDirectoryDto>> GetAllContactsAsync()
        {
            EnsureAdminOrOwner();
            var bId =  _authUser.CompanyBranchId
                       ?? throw new InvalidOperationException("BranchId missing");

            var contacts = await _context.ContactDirectories
                                .AsNoTracking()
                                .Include(c => c.Category)
                                .Include(c => c.CompanyBranch)
                                .Where(c => c.CompanyBranchId == bId)
                                .OrderByDescending(c => c.CreatedDate)
                                .ToListAsync();

            return contacts
                .Select(c => new ContactDirectoryDto
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
                CreatedDate = c.CreatedDate
            }).ToList();
        }




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
