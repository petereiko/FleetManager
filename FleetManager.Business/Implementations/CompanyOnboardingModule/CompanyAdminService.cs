using FleetManager.Business.Database.Entities;
using FleetManager.Business.Database.IdentityModels;
using FleetManager.Business.DataObjects;
using FleetManager.Business.Enums;
using FleetManager.Business.Implementations.CompanyModule;
using FleetManager.Business.Interfaces.CompanyModule;
using FleetManager.Business.Interfaces.CompanyOnboardingModule;
using FleetManager.Business.Interfaces.EmailModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.CompanyOnboardingModule
{
    public class CompanyAdminService : ICompanyAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly FleetManagerDbContext _context;
        private readonly ICompanyManagementService _companyService;
        private readonly IEmailService _emailService;
        private readonly ILogger<CompanyAdminService> _logger;
        private readonly IAuthUser _authUser;


        public CompanyAdminService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            FleetManagerDbContext context,
            IEmailService emailService,
            ILogger<CompanyAdminService> logger,
            IAuthUser authUser,
            ICompanyManagementService companyService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _authUser = authUser;
            _companyService = companyService;
        }

        public async Task<IEnumerable<CompanyAdminDto>> GetAllAdminsAsync()
        {
            // 1) Get current company from your UserData helper:
            var userData = _companyService.GetUserData();
            if (userData?.CompanyId == null)
                return new List<CompanyAdminDto>();

            long companyId = userData.CompanyId;

            // 2) Join CompanyAdmins, Users, and Branches:
            var admins = await (
                from ca in _context.CompanyAdmins.AsNoTracking()
                where ca.CompanyId == companyId
                join u in _context.Users.AsNoTracking() on ca.UserId equals u.Id
                join b in _context.CompanyBranches.AsNoTracking() on ca.CompanyBranchId equals b.Id into bj
                from branch in bj.DefaultIfEmpty()
                select new CompanyAdminDto
                {
                    Id = ca.Id,
                    UserId = u.Id,
                    FirstName = u.FirstName ?? "",
                    LastName = u.LastName ?? "",
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber ?? "",
                    BranchName = branch != null ? branch.Name : "(no branch)",
                    IsActive = ca.IsActive,
                    CreatedDate = ca.CreatedDate
                }
            ).ToListAsync();

            return admins;
        }

        public async Task<MessageResponse> OnboardCompanyAdminAsync(CompanyAdminOnboardingDto dto, string createdByUserId)
        {
            var response = new MessageResponse();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(dto.Email);
                if (existingUser != null)
                {
                    response.Message = $"A user with email '{dto.Email}' already exists.";
                    return response;
                }

                // Validate company branch
                var branch = await _context.CompanyBranches
                    .FirstOrDefaultAsync(b => b.Id == dto.CompanyBranchId && b.CompanyId == dto.CompanyId);

                if (branch == null)
                {
                    response.Message = "Invalid company branch. Please select a valid branch for this company.";
                    return response;
                }

                // Create ApplicationUser
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
                    CompanyId = dto.CompanyId,
                    CompanyBranchId = dto.CompanyBranchId
                };

                var tempPassword = Guid.NewGuid().ToString("N")[..8];

                var userResult = await _userManager.CreateAsync(user, tempPassword);
                if (!userResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    response.Message = userResult.Errors.FirstOrDefault()?.Description ?? "User creation failed.";
                    return response;
                }

                // 🎯 Pull the correct role name from the enum’s Description
                var companyAdminRoleName = EnumHelper<Role>.GetDescription(Role.CompanyAdmin);

                var roleAssignResult = await _userManager.AddToRoleAsync(user, companyAdminRoleName);
                if (!roleAssignResult.Succeeded)
                {
                    response.Message = $"Failed to assign role '{companyAdminRoleName}'.";
                    await transaction.RollbackAsync();
                    return response;
                }
               
                var admin = new CompanyAdmin
                {
                    UserId = user.Id,
                    CompanyId = dto.CompanyId,
                    CompanyBranchId = dto.CompanyBranchId,
                    IsActive = true,
                    CreatedBy = createdByUserId,
                    CreatedDate = DateTime.UtcNow
                };

                _context.CompanyAdmins.Add(admin);
                await _context.SaveChangesAsync();

                // Send Email
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = WebUtility.UrlEncode(token);
                var confirmUrl = $"{_authUser.BaseUrl}/Account/ConfirmEmail?encodedToken={encodedToken}&userId={user.Id}";

                string message = $@"
                                    <div style='font-family:Segoe UI, sans-serif; font-size:15px; color:#333;'>
                                        <p>Dear {dto.FirstName},</p>
                                        <p>You’ve been onboarded as a <strong>Company Admin</strong> on Driver's Sight.</p>
                                        <ul>
                                            <li><strong>Email:</strong> {dto.Email}</li>
                                            <li><strong>Temporary Password:</strong> <span style='color:#007BFF;'>{tempPassword}</span></li>
                                        </ul>
                                        <p>Click below to confirm your email:</p>
                                        <p style='text-align:center;'>
                                            <a href='{confirmUrl}' style='background:#28a745; padding:10px 20px; color:white; border-radius:4px; text-decoration:none;'>Confirm Email</a>
                                        </p>
                                        <p>After confirming, log in <a href='{_authUser.BaseUrl}/Account/Login'>here</a> and change your password.</p>
                                        <p>Thank you,<br/><strong>Driver's Sight Team</strong></p>
                                    </div>";

                var emailSuccess = await _emailService.LogEmail(new EmailLogDto
                {
                    CompanyId = dto.CompanyId,
                    CompanyBranchId = dto.CompanyBranchId,
                    Email = dto.Email,
                    Subject = "Admin Onboarding",
                    Message = message,
                    Url = confirmUrl,

                });

                if (!emailSuccess)
                {
                    await transaction.RollbackAsync();
                    response.Message = "Admin created but email could not be sent.";
                    return response;
                }

                await transaction.CommitAsync();

                response.Success = true;
                response.Message = $"Admin '{dto.Email}' created successfully. Login credentials have been sent.";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to onboard company admin.");
                await transaction.RollbackAsync();

                response.Message = "An unexpected error occurred while onboarding the admin.";
                return response;
            }
        }

    }
}
