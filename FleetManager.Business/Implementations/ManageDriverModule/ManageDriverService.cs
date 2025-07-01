using FleetManager.Business.Database.Entities;
using FleetManager.Business.Database.IdentityModels;
using FleetManager.Business.DataObjects;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.EmailModule;
using FleetManager.Business.Interfaces.ManageDriverModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Http;
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
using System.Xml.Linq;

namespace FleetManager.Business.Implementations.ManageDriverModule
{
    public class ManageDriverService:IManageDriverService
    {
        private readonly FleetManagerDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IAuthUser _authUser;
        private readonly ILogger<ManageDriverService> _logger;

        public ManageDriverService(
            FleetManagerDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IAuthUser authUser,
            ILogger<ManageDriverService> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _authUser = authUser;
            _logger = logger;
        }

        private void EnsureAdminOrOwner()
        {
            var roles = (_authUser.Roles ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim());

            if (!roles.Contains("Company Admin") &&
                !roles.Contains("Company Owner") &&
                !roles.Contains("Super Admin"))
            {
                throw new UnauthorizedAccessException("Insufficient permissions to manage drivers.");
            }
        }

        public async Task<MessageResponse> OnboardDriverAsync(DriverOnboardingDto dto, string createdByUserId)
        {
            EnsureAdminOrOwner();
            var resp = new MessageResponse();
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1) user exists?
                if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                {
                    resp.Message = $"Email '{dto.Email}' already in use.";
                    return resp;
                }

                // 2) validate branch
                var branch = await _context.CompanyBranches
                    .FirstOrDefaultAsync(b => b.Id == dto.CompanyBranchId
                                           && b.CompanyId == _authUser.CompanyId);
                if (branch == null)
                {
                    resp.Message = "Invalid branch.";
                    return resp;
                }

                // 3) create ApplicationUser
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
                    CompanyId = branch.CompanyId,
                    CompanyBranchId = branch.Id
                };
                var tempPwd = Guid.NewGuid().ToString("N").Substring(0, 8);
                var createResult = await _userManager.CreateAsync(user, tempPwd);
                if (!createResult.Succeeded)
                {
                    resp.Message = createResult.Errors.First().Description;
                    return resp;
                }

                // 4) assign Driver role
                var driverRole = EnumHelper<Role>.GetDescription(Role.Driver);
                var roleResult = await _userManager.AddToRoleAsync(user, driverRole);
                if (!roleResult.Succeeded)
                {
                    resp.Message = "Could not assign driver role.";
                    await tx.RollbackAsync();
                    return resp;
                }

                // 5) persist Driver entity
                var driver = new Driver
                {
                    UserId = user.Id,
                    Address = dto.Address,
                    DateOfBirth = dto.DateOfBirth,
                    Gender = dto.Gender,
                    EmploymentStatus = dto.EmploymentStatus,
                    LicenseNumber = dto.LicenseNumber,
                    LicenseExpiryDate = dto.LicenseExpiryDate,
                    CompanyBranchId = dto.CompanyBranchId,
                    LicenseCategory = dto.LicenseCategory,
                    ShiftStatus = dto.ShiftStatus,
                    CreatedBy = createdByUserId,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };
                _context.Drivers.Add(driver);
                await _context.SaveChangesAsync();

                // 6) handle uploads
                var docs = new List<DriverDocument>();
                if (dto.LicensePhoto != null)
                    docs.Add(await SaveDriverFileAsync(driver.Id, dto.LicensePhoto, "License"));
                if (dto.ProfilePhoto != null)
                    docs.Add(await SaveDriverFileAsync(driver.Id, dto.ProfilePhoto, "Profile"));

                if (docs.Any())
                {
                    _context.DriverDocuments.AddRange(docs);
                    await _context.SaveChangesAsync();
                }

                // 7) send email
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = WebUtility.UrlEncode(token);
                var confirmUrl = $"{_authUser.BaseUrl}/Account/ConfirmEmail?encodedToken={encodedToken}&userId={user.Id}";

                await _emailService.LogEmail(new EmailLogDto
                {
                    CompanyId = branch.CompanyId,
                    CompanyBranchId = dto.CompanyBranchId,
                    Email = dto.Email,
                    Subject = "Driver Account Created",
                    Message = $"Dear {dto.FirstName},<br/>Your account is ready. PW: <b>{tempPwd}</b><br/><a href='{confirmUrl}'>Confirm Email</a>",
                    Url = confirmUrl
                });

                await tx.CommitAsync();
                resp.Success = true;
                resp.Message = "Driver onboarded successfully.";
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnboardDriverAsync failed");
                await tx.RollbackAsync();
                resp.Message = "Unexpected error.";
                return resp;
            }
        }

        public async Task<MessageResponse<DriverDto>> UpdateDriverAsync(DriverDto dto, string modifiedByUserId)
        {
            EnsureAdminOrOwner();

            var resp = new MessageResponse<DriverDto>();
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var entity = await _context.Drivers.FindAsync(dto.Id);
                if (entity == null)
                {
                    resp.Message = "Driver not found.";
                    return resp;
                }

                // map changes
                entity.Address = dto.Address;
                entity.DateOfBirth = dto.DateOfBirth;
                entity.Gender = dto.Gender;
                entity.EmploymentStatus = dto.EmploymentStatus;
                entity.LicenseNumber = dto.LicenseNumber;
                entity.LicenseExpiryDate = dto.LicenseExpiryDate;
                entity.LicenseCategory = dto.LicenseCategory;
                entity.ShiftStatus = dto.ShiftStatus;
                entity.IsActive = dto.IsActive;

                entity.ModifiedBy = modifiedByUserId;
                entity.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync().ConfigureAwait(false);
                await tx.CommitAsync().ConfigureAwait(false);

                resp.Success = true;
                resp.Result = dto;
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating driver");
                await tx.RollbackAsync();
                resp.Message = "An error occurred updating the driver.";
                return resp;
            }
        }

        public async Task<MessageResponse> DeleteDriverAsync(long id)
        {
            EnsureAdminOrOwner();

            var resp = new MessageResponse();
            try
            {
                var entity = await _context.Drivers.FindAsync(id);
                if (entity == null)
                {
                    resp.Message = "Driver not found.";
                    return resp;
                }

                _context.Drivers.Remove(entity);
                await _context.SaveChangesAsync().ConfigureAwait(false);

                resp.Success = true;
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting driver");
                resp.Message = "An error occurred deleting the driver.";
                return resp;
            }
        }

        public async Task<DriverDto?> GetDriverByIdAsync(long id)
        {
            EnsureAdminOrOwner();

            // 1) load driver core info
            var d = await _context.Drivers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id)
                .ConfigureAwait(false);

            if (d == null) return null;

            // 2) load related ASP-Identity user
            //    we assume ApplicationUser.Id == d.Id.ToString()
            var userId = await _context.Users
                .Where(u => u.CompanyBranchId == d.CompanyBranchId && u.Id == d.Id.ToString())
                .Select(u => u.Id)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            var u = await _userManager.FindByIdAsync(userId)
                .ConfigureAwait(false);

            // 3) load all driver documents (both license and profile photos)
            var docs = await _context.DriverDocuments
                .AsNoTracking()
                .Where(doc => doc.DriverId == id)
                .ToListAsync()
                .ConfigureAwait(false);

            // 4) map into DTO
            return new DriverDto
            {
                Id = d.Id,
                FirstName = u?.FirstName ?? string.Empty,
                LastName = u?.LastName ?? string.Empty,
                Email = u?.Email ?? string.Empty,
                PhoneNumber = u?.PhoneNumber ?? string.Empty,
                Address = d.Address,
                DateOfBirth = d.DateOfBirth,
                Gender = d.Gender,
                EmploymentStatus = d.EmploymentStatus,
                LicenseNumber = d.LicenseNumber,
                LicenseExpiryDate = d.LicenseExpiryDate,
                CompanyBranchId = d.CompanyBranchId ?? 0,
                LicenseCategory = d.LicenseCategory,
                ShiftStatus = d.ShiftStatus,
                IsActive = d.IsActive,
                CreatedDate = d.CreatedDate,

                // split out your two doc‐types:
                Documents = docs
                  .Where(x => x.DocumentType == DriverDocumentType.LicensePhoto)
                  .Select(x => new DriverDocumentDto
                  {
                      Id = x.Id,
                      FileName = x.FileName,
                      FilePath = x.FilePath,
                      UploadedDate=x.CreatedDate
                  })
                  .ToList(),

                 Photos = docs
                  .Where(x => x.DocumentType == DriverDocumentType.ProfilePhoto)
                  .Select(x => new DriverDocumentDto
                  {
                      Id = x.Id,
                      FileName = x.FileName,
                      FilePath = x.FilePath,
                      UploadedDate = x.CreatedDate
                  })
                  .ToList()
            };
        }

        public async Task<List<DriverDto>> GetDriversForBranchAsync(long? branchId = null)
        {
            EnsureAdminOrOwner();

            var companyId = _authUser.CompanyId;
            var isSuperAdmin = (_authUser.Roles ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .Any(r => r == "Company Owner" || r == "Super Admin");

            // Step 1: Join Driver and ApplicationUser
            var query = from d in _context.Drivers.AsNoTracking()
                        join u in _context.Users.AsNoTracking()
                            on d.UserId equals u.Id
                        where u.CompanyId == companyId
                        select new { Driver = d, User = u };

            if (!isSuperAdmin)
            {
                query = query.Where(x => x.Driver.CompanyBranchId == _authUser.CompanyBranchId);
            }

            if (branchId.HasValue)
            {
                query = query.Where(x => x.Driver.CompanyBranchId == branchId.Value);
            }

            // Step 2: Materialize data from DB
            var driverData = await query.ToListAsync().ConfigureAwait(false);
            var driverIds = driverData.Select(x => x.Driver.Id).ToList();

            // Step 3: Get all driver documents at once
            var allDocs = await _context.DriverDocuments
                .AsNoTracking()
                .Where(doc => doc.DriverId.HasValue && driverIds.Contains(doc.DriverId.Value))
                .ToListAsync()
                .ConfigureAwait(false);

            // Step 4: Map everything into DriverDto
            var result = driverData.Select(x =>
            {
                var docs = allDocs.Where(d => d.DriverId == x.Driver.Id).ToList();

                return new DriverDto
                {
                    Id = x.Driver.Id,
                    FirstName = x.User.FirstName ?? "",
                    LastName = x.User.LastName ?? "",
                    Email = x.User.Email ?? "",
                    PhoneNumber = x.User.PhoneNumber ?? "",
                    FullName = x.User.FirstName + " " + x.User.LastName,
                    Address = x.Driver.Address,
                    DateOfBirth = x.Driver.DateOfBirth,
                    Gender = x.Driver.Gender,
                    EmploymentStatus = x.Driver.EmploymentStatus,
                    LicenseNumber = x.Driver.LicenseNumber,
                    LicenseExpiryDate = x.Driver.LicenseExpiryDate,
                    CompanyBranchId = x.Driver.CompanyBranchId ?? 0,
                    LicenseCategory = x.Driver.LicenseCategory,
                    ShiftStatus = x.Driver.ShiftStatus,
                    IsActive = x.Driver.IsActive,
                    CreatedDate = x.Driver.CreatedDate,

                    Documents = docs
                        .Where(d => d.DocumentType == DriverDocumentType.LicensePhoto)
                        .Select(d => new DriverDocumentDto
                        {
                            Id = d.Id,
                            FileName = d.FileName,
                            FilePath = d.FilePath,
                            UploadedDate = d.CreatedDate
                        }).ToList(),

                    Photos = docs
                        .Where(d => d.DocumentType == DriverDocumentType.ProfilePhoto)
                        .Select(d => new DriverDocumentDto
                        {
                            Id = d.Id,
                            FileName = d.FileName,
                            FilePath = d.FilePath,
                            UploadedDate = d.CreatedDate
                        }).ToList()
                };
            }).ToList();

            return result;
        }

        // helper to save file
        private async Task<DriverDocument> SaveDriverFileAsync(
            long driverId, IFormFile file, string subfolder)
        {
            var uploadRoot = Path.Combine(Directory.GetCurrentDirectory(),
                                          "wwwroot", "DriverImages", subfolder);
            Directory.CreateDirectory(uploadRoot);

            var unique = $"{Guid.NewGuid()}_{file.FileName}";
            var full = Path.Combine(uploadRoot, unique);
            using var stream = new FileStream(full, FileMode.Create);
            await file.CopyToAsync(stream);

            return new DriverDocument
            {
                DriverId = driverId,
                FileName = file.FileName,
                CreatedDate = DateTime.UtcNow,
                FilePath = $"/DriverImages/{subfolder}/{unique}",
                DocumentType = subfolder == "Profile"
                    ? DriverDocumentType.ProfilePhoto
                    : DriverDocumentType.LicensePhoto
            };
        }

        public IQueryable<DriverListItemDto> QueryDriversForBranch(long? branchId)
        {
            EnsureAdminOrOwner();

            // Start from Drivers table
            var q = from d in _context.Drivers.AsNoTracking()
                    where !branchId.HasValue || d.CompanyBranchId == branchId.Value
                    join u in _context.Users.AsNoTracking()
                        on d.UserId equals u.Id

                    // Left join to DriverDocuments to get the profile photo
                    join doc in _context.DriverDocuments.AsNoTracking()
                        .Where(dd => dd.DocumentType == DriverDocumentType.ProfilePhoto)
                        on d.Id equals doc.DriverId into docJoin
                    from profilePhoto in docJoin.DefaultIfEmpty()

                    select new DriverListItemDto
                    {
                        Id = d.Id,
                        FullName = (u.FirstName ?? "") + " " + (u.LastName ?? ""),
                        LicenseNumber = d.LicenseNumber,
                        ShiftStatus = d.ShiftStatus,
                        EmploymentStatus = d.EmploymentStatus,
                        Email = u.Email,
                        Phone = u.PhoneNumber,
                        IsActive = d.IsActive,
                        CreatedDate = d.CreatedDate,
                        VehicleAssigned = "No Vehicle Assigned Yet",

                        // 👇 Attach profile photo path if available
                        PhotoPath = profilePhoto != null ? profilePhoto.FilePath : null
                    };


            return q;
        }



        //public async Task<List<DriverDto>> GetDriversForBranchAsync(long? branchId = null)
        //{
        //    EnsureAdminOrOwner();

        //    var companyId = _authUser.CompanyId;
        //    var query = _context.Drivers.AsNoTracking();

        //    // if owner/super: all company, else branch only
        //    var roles = (_authUser.Roles ?? "")
        //        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        //        .Select(r => r.Trim());

        //    if (!roles.Contains("Company Owner") && !roles.Contains("Super Admin"))
        //        query = query.Where(d => d.CompanyBranchId == _authUser.CompanyBranchId);

        //    var list = await query
        //        .Where(d => branchId == null || d.CompanyBranchId == branchId)
        //        .ToListAsync()
        //        .ConfigureAwait(false);

        //    // map
        //    var result = new List<DriverDto>(list.Count);
        //    foreach (var d in list)
        //        result.Add(await GetDriverByIdAsync(d.Id).ConfigureAwait(false)!);

        //    return result;
        //}





        public List<SelectListItem> GetGenderOptions() =>
        Enum.GetValues<Gender>()
            .Cast<Gender>()
            .Select(e => new SelectListItem
            {
                Value = ((int)e).ToString(),
                Text = e.ToString()
            })
            .ToList();

        public List<SelectListItem> GetEmploymentStatusOptions() =>
            Enum.GetValues<EmploymentStatus>()
                .Cast<EmploymentStatus>()
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e.ToString()
                })
                .ToList();

        public List<SelectListItem> GetShiftStatusOptions() =>
            Enum.GetValues<ShiftStatus>()
                .Cast<ShiftStatus>()
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e.ToString()
                })
                .ToList();

        public List<SelectListItem> GetLicenseCategoryOptions() =>
            Enum.GetValues<LicenseCategory>()
                .Cast<LicenseCategory>()
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e.ToString()
                })
                .ToList();
    }
}
