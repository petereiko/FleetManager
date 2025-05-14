using FleetManager.Business.Database.Entities;
using FleetManager.Business.Database.IdentityModels;
using FleetManager.Business.DataObjects;
using FleetManager.Business.Enums;
using FleetManager.Business.Implementations.UserModule;
using FleetManager.Business.Interfaces.CompanyModule;
using FleetManager.Business.Interfaces.EmailModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.UtilityModels;
using FleetManager.Business.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.CompanyModule
{
    public class CompanyService : ICompanyService
    {

        private readonly FleetManagerDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<CompanyService> _logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        private readonly IMemoryCache _memoryCache;
        private readonly IAuthUser _authUser;
        public CompanyService(FleetManagerDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, SignInManager<ApplicationUser> signInManager, ILogger<CompanyService> logger, IHttpContextAccessor contextAccessor, IEmailService emailService, IConfiguration configuration, IMemoryCache memoryCache, IAuthUser authUser)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _logger = logger;
            _contextAccessor = contextAccessor;
            _emailService = emailService;
            _configuration = configuration;
            _memoryCache = memoryCache;
            _authUser = authUser;
        }
        
       

        public async Task<MessageResponse> OnboardCompany(CompanyRegistrationViewModel model)
        {
            MessageResponse response = new();

            if (!model.HasAgreed)
            {
                response.Message = "You must agree to the terms and conditions.";
                return response;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    response.Message = $"A user with the email {model.Email} already exists.";
                    return response;
                }

                var company = new Company
                {
                    Name = model.Name!,
                    RegistrationNumber = model.RegistrationNumber,
                    Address = model.Address!,
                    DateOfIncorporation = model.DateOfIncorporation,
                    State = model.State,
                    Email = model.Email!,
                    PhoneNumber = model.PhoneNumber!,
                    ContactPersonName = model.ContactPersonName,
                    ContactPersonPhone = model.ContactPersonPhone,
                    ContactPersonEmail = model.ContactPersonEmail,
                    Website = model.Website,
                    LogoUrl = model.LogoUrl,
                    IsVerified = model.IsVerified,
                    Token = model.Token
                };

                await _context.Companies.AddAsync(company);
                await _context.SaveChangesAsync();

                // 3. Create the ApplicationUser (admin)
                var user = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = model.Email,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    IsFirstLogin = true,
                    CompanyId = company.Id,
                    EmailConfirmed = false 
                };

                string password = Guid.NewGuid().ToString("N")[..8]; // 8-char password
                var identityResult = await _userManager.CreateAsync(user, password);
                if (!identityResult.Succeeded)
                {
                    response.Message = identityResult.Errors.FirstOrDefault()?.Description ?? "User creation failed";
                    await transaction.RollbackAsync();
                    return response;
                }

                var roleAssignResult = await _userManager.AddToRoleAsync(user, EnumHelper<Role>.GetDescription(Role.CompanyAdmin));
                if (!roleAssignResult.Succeeded)
                {
                    response.Message = "An error occurred.";
                    await transaction.RollbackAsync();
                    return response;
                }

                // 5. Email confirmation
                string confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = WebUtility.UrlEncode(confirmationToken);
                string url = $"{_authUser.BaseUrl}/User/ConfirmEmail?encodedToken={encodedToken}&userId={user.Id}";


                string message = $@"
    <div style='font-family:Segoe UI, sans-serif; font-size:15px; color:#333;'>
        <p>Dear {model.ContactPersonName},</p>

        <p>Welcome to <strong>Driver's Sight</strong>! 🎉</p>

        <p>
            Your company <strong>{model.Name}</strong> has been successfully registered on our platform.<br/>
            As the company administrator, your account has been created with the following details:
        </p>

        <ul style='line-height:1.6;'>
            <li><strong>Email:</strong> {model.Email}</li>
            <li><strong>Temporary Password:</strong> <span style='font-weight:bold; color:#007BFF;'>{password}</span></li>
        </ul>
<p>
            Before you can start using the platform, please confirm your email address by clicking the button below:
        </p>

        <p style='text-align:center; margin:20px 0;'>
            <a href='{url}' style='display:inline-block; padding:10px 20px; background-color:#28a745; color:#fff;
            text-decoration:none; border-radius:5px;'>Confirm My Email</a>
        </p>

        <p>
            Please <a href='{_authUser.BaseUrl}/Account/Login' style='color:#007BFF;'>log in here</a> to access your dashboard.
            Upon login, you will be prompted to update your password.
        </p>

        

        <p>If you did not initiate this registration, please ignore this message.</p>

        <p>Thank you,<br/><strong>The Driver's Sight Team</strong></p>
    </div>
";

                bool emailLogSuccess = await _emailService.LogEmail(new EmailLogDto
                {
                    Email = model.Email,
                    Message = message,
                    Subject = "Company Admin Account Created",
                    Url = url
                });

                if (!emailLogSuccess)
                {
                    response.Message = "Failed to send confirmation email.";
                    await transaction.RollbackAsync();
                    return response;
                }

                await transaction.CommitAsync();
                response.Success = true;
                response.Message = $"Company registered and admin account created successfully. A confirmation email has been sent to {model.Email}.";

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error onboarding company.");
                await transaction.RollbackAsync();
                response.Message = "An unexpected error occurred while registering the company.";
                return response;
            }
        }



        public async Task<MessageResponse> ConfirmEmail(string encodedToken, string userId)
        {

            MessageResponse response = new();
            if (string.IsNullOrWhiteSpace(encodedToken) || string.IsNullOrWhiteSpace(userId))
            {
                response.Success = false;
                response.Message = "Invalid token or user ID";
                return response;
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                response.Message = "User not found";
                return response;
            }

            var token = WebUtility.UrlDecode(encodedToken);
            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (!result.Succeeded)
            {
                response.Message = "Email confirmation failed";
                return response;
            }

            user.EmailConfirmed = true;
            //user.IsFirstLogin = false;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            response.Success = true;
            response.Message = "Email confirmed successfully. You can now log in.";
            return response;
        }


    }
}
