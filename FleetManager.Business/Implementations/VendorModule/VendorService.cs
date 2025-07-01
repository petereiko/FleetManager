using FleetManager.Business.Database.Entities;
using FleetManager.Business.Database.IdentityModels;
using FleetManager.Business.DataObjects;
using FleetManager.Business.DataObjects.VendorDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.EmailModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.Interfaces.VendorModule;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.VendorModule
{
    public class VendorService : IVendorService
    {
        private readonly FleetManagerDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IAuthUser _authUser;
        private readonly ILogger<VendorService> _logger;

        public VendorService(
            FleetManagerDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IAuthUser authUser,
            ILogger<VendorService> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _authUser = authUser;
            _logger = logger;
        }

        public async Task<MessageResponse> OnboardVendorAsync(VendorOnboardingDto dto)
        {
            var resp = new MessageResponse();
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                {
                    resp.Message = $"Email '{dto.Email}' already in use.";
                    return resp;
                }

                var user = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = dto.Email,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    IsActive = true,
                    IsFirstLogin = true,
                    CreatedDate = DateTime.UtcNow,
                    CompanyId = null,
                    CompanyBranchId = null,
                    VendorId = null
                };
                var tempPwd = Guid.NewGuid().ToString("N")[..8];
                var createResult = await _userManager.CreateAsync(user, tempPwd);
                if (!createResult.Succeeded)
                {
                    resp.Message = createResult.Errors.First().Description;
                    return resp;
                }

                var vendorRole = EnumHelper<Role>.GetDescription(Role.Vendor);
                var roleResult = await _userManager.AddToRoleAsync(user, vendorRole);
                if (!roleResult.Succeeded)
                {
                    resp.Message = "Could not assign vendor role.";
                    await tx.RollbackAsync();
                    return resp;
                }

                var vendor = new Vendor
                {
                    UserId = user.Id,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    VendorName = dto.VendorName,
                    VendorCategoryId = dto.VendorCategoryId,
                    ContactPerson = dto.ContactPerson,
                    ContactPersonPhone = dto.ContactPersonPhone,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    Address = dto.Address,
                    VendorServiceOffered = dto.VendorServiceOffered,
                    CACRegistrationNo = dto.CACRegistrationNo,
                    TaxIdNumber = dto.TaxIdNumber,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };
                _context.Vendors.Add(vendor);
                await _context.SaveChangesAsync();

                user.VendorId = vendor.Id;
                await _userManager.UpdateAsync(user);

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = WebUtility.UrlEncode(token);
                var confirmUrl = $"{_authUser.BaseUrl}/Account/ConfirmEmail?encodedToken={encodedToken}&userId={user.Id}";

                await _emailService.LogEmail(new EmailLogDto
                {
                    CompanyId = null,
                    CompanyBranchId = null,
                    VendorId = vendor.Id,
                    Email = dto.Email,
                    Subject = "Vendor Account Created",
                    Message = $"Dear {dto.FirstName} {dto.LastName},<br/>Your vendor account is ready. PW: <b>{tempPwd}</b><br/><a href='{confirmUrl}'>Confirm Email</a>",
                    Url = confirmUrl
                });

                await tx.CommitAsync();
                resp.Success = true;
                resp.Message = "Vendor onboarded successfully.";
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnboardVendorAsync failed");
                await tx.RollbackAsync();
                resp.Message = "Unexpected error.";
                return resp;
            }
        }

        public async Task<MessageResponse<VendorDto>> UpdateVendorAsync(VendorDto dto)
        {
            var resp = new MessageResponse<VendorDto>();
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var entity = await _context.Vendors
                    .Include(v => v.User)
                    .FirstOrDefaultAsync(v => v.Id == dto.Id);
                if (entity == null)
                {
                    resp.Message = "Vendor not found.";
                    return resp;
                }

                entity.FirstName = dto.FirstName;
                entity.LastName = dto.LastName;
                entity.VendorName = dto.VendorName;
                entity.VendorCategoryId = dto.VendorCategoryId;
                entity.ContactPerson = dto.ContactPerson;
                entity.ContactPersonPhone = dto.ContactPersonPhone;
                entity.Email = dto.Email;
                entity.PhoneNumber = dto.PhoneNumber;
                entity.Address = dto.Address;
                entity.VendorServiceOffered = dto.VendorServiceOffered;
                entity.CACRegistrationNo = dto.CACRegistrationNo;
                entity.TaxIdNumber = dto.TaxIdNumber;
                entity.IsActive = dto.IsActive;
                entity.ModifiedBy = _authUser.UserId;
                entity.ModifiedDate = DateTime.UtcNow;

                if (entity.User != null)
                {
                    entity.User.Email = dto.Email;
                    entity.User.PhoneNumber = dto.PhoneNumber;
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                resp.Success = true;
                resp.Result = dto;
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateVendorAsync failed");
                await tx.RollbackAsync();
                resp.Message = "An error occurred updating the vendor.";
                return resp;
            }
        }

        public async Task<MessageResponse> DeleteVendorAsync(long id)
        {
            var resp = new MessageResponse();
            try
            {
                var entity = await _context.Vendors.FindAsync(id);
                if (entity == null)
                {
                    resp.Message = "Vendor not found.";
                    return resp;
                }

                _context.Vendors.Remove(entity);
                await _context.SaveChangesAsync();

                resp.Success = true;
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteVendorAsync failed");
                resp.Message = "An error occurred deleting the vendor.";
                return resp;
            }
        }

        public async Task<VendorDto?> GetVendorByIdAsync(long id)
        {
            var v = await _context.Vendors
                .AsNoTracking()
                .Include(v => v.User)
                .Include(v => v.VendorCategory)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (v == null) return null;

            return new VendorDto
            {
                Id = v.Id,
                FirstName = v.FirstName,
                LastName = v.LastName,
                VendorName = v.VendorName,
                VendorCategoryId = v.VendorCategoryId,
                VendorCategoryName = v.VendorCategory.Name,
                VendorServiceOffered = v.VendorServiceOffered,
                ContactPerson = v.ContactPerson,
                ContactPersonPhone = v.ContactPersonPhone,
                Address = v.Address,
                Email = v.Email,
                PhoneNumber = v.PhoneNumber,
                CACRegistrationNo = v.CACRegistrationNo,
                TaxIdNumber = v.TaxIdNumber,
                IsActive = v.IsActive,
                CreatedDate = v.CreatedDate,
                CreatedBy = v.CreatedBy,
                ModifiedDate = v.ModifiedDate,
                ModifiedBy = v.ModifiedBy
            };
        }

        public async Task<List<VendorDto>> GetVendorsAsync()
        {
            var list = await _context.Vendors
                .AsNoTracking()
                .Include(v => v.VendorCategory)
                .ToListAsync();

            return list.Select(v => new VendorDto
            {
                Id = v.Id,
                FirstName = v.FirstName,
                LastName = v.LastName,
                VendorName = v.VendorName,
                VendorCategoryId = v.VendorCategoryId,
                VendorCategoryName = v.VendorCategory.Name,
                VendorServiceOffered = v.VendorServiceOffered,
                ContactPerson = v.ContactPerson,
                ContactPersonPhone = v.ContactPersonPhone,
                Address = v.Address,
                Email = v.Email,
                PhoneNumber = v.PhoneNumber,
                CACRegistrationNo = v.CACRegistrationNo,
                TaxIdNumber = v.TaxIdNumber,
                IsActive = v.IsActive,
                CreatedDate = v.CreatedDate
            }).ToList();
        }

        public List<SelectListItem> GetVendorCategoryOptions()
            => _context.VendorCategories
                .AsNoTracking()
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToList();

        //public List<SelectListItem> GetVendorServiceOptions()
        //    => _context.VendorServicesOffered
        //        .AsNoTracking()
        //        .Select(s => new SelectListItem
        //        {
        //            Value = s.Id.ToString(),
        //            Text = s.Name
        //        })
        //        .ToList();
    }
}

