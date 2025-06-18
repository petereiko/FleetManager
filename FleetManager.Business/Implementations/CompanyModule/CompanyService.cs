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
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.CompanyModule
{
    public class CompanyService : ICompanyManagementService
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



                var companyBranch = new CompanyBranch
                {
                    Name = model.Name!,
                    CompanyId = company.Id,
                    Address = model.Address!,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = null,
                    Email = model.Email!,
                    IsHeadOffice = true,
                    IsActive = true,
                    LgaId = null,
                    ManagerEmail = null,
                    ManagerName = null,
                    StateId = null,
                    Phone = null,
                    ManagerPhone = null
                };
                _context.CompanyBranches.Add(companyBranch);
                await _context.SaveChangesAsync();


                // 3. Create the ApplicationUser (Owner)
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
                    EmailConfirmed = false,
                    CompanyBranchId = companyBranch.Id
                };

                string password = Guid.NewGuid().ToString("N")[..8]; // 8-char password
                var identityResult = await _userManager.CreateAsync(user, password);
                if (!identityResult.Succeeded)
                {
                    response.Message = identityResult.Errors.FirstOrDefault()?.Description ?? "User creation failed";
                    await transaction.RollbackAsync();
                    return response;
                }

                companyBranch.CreatedBy = user.Id;
                _context.CompanyBranches.Update(companyBranch);
                await _context.SaveChangesAsync();

                var roleAssignResult = await _userManager.AddToRoleAsync(user, EnumHelper<Role>.GetDescription(Role.CompanyOwner));
                if (!roleAssignResult.Succeeded)
                {
                    response.Message = "An error occurred.";
                    await transaction.RollbackAsync();
                    return response;
                }

               
                // 5. Email confirmation
                string confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = WebUtility.UrlEncode(confirmationToken);
                string url = $"{_authUser.BaseUrl}/Company/User/ConfirmEmail?encodedToken={encodedToken}&userId={user.Id}";


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
                    CompanyId = company.Id,
                    CompanyBranchId = companyBranch.Id,
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

            //var token = WebUtility.UrlDecode(encodedToken);
            var result = await _userManager.ConfirmEmailAsync(user, encodedToken);

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

        #region UserData

        //public UserViewModel GetUserData()
        //{
        //    UserViewModel userData = new();
        //    string userDataString = null;
        //    if (_contextAccessor.HttpContext == null)
        //    {
        //        return userData;
        //    }
        //    _contextAccessor.HttpContext.Request.Cookies.TryGetValue(AppConstants.CACHEUSERDATA, out userDataString);

        //    if (string.IsNullOrEmpty(userDataString))
        //    {
        //        string email = _contextAccessor.HttpContext.User.Identity.Name;
        //        userData = GetStaffByEmail(email).GetAwaiter().GetResult();

        //        //string userDataJson = JsonConvert.SerializeObject(userData);

        //        //_contextAccessor.HttpContext.Response.Cookies.Append(AppConstants.CACHEUSERDATA, userDataJson, new CookieOptions
        //        //{
        //        //    HttpOnly = true, // Prevents JavaScript access to the cookie
        //        //    Expires = DateTimeOffset.UtcNow.AddMinutes(1) // Set an expiration
        //        //});
        //    }
        //    else
        //    {
        //        // Deserialize the JSON string back to the object
        //        userData = JsonConvert.DeserializeObject<UserViewModel>(userDataString);
        //        // Use userData as needed
        //    }

        //    return userData;
        //}

        #endregion

        public UserViewModel GetUserData()
        {
            var httpContext = _contextAccessor.HttpContext;
            if (httpContext == null || !httpContext.User.Identity.IsAuthenticated)
                throw new InvalidOperationException("User is not authenticated.");

            var user = httpContext.User;
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new InvalidOperationException("Missing NameIdentifier claim.");
            var email = user.FindFirst(ClaimTypes.Email)?.Value
                ?? throw new InvalidOperationException("Missing Email claim.");
            var fullName = user.FindFirst("FullName")?.Value ?? "";

            // --- Required CompanyId ---
            var companyClaim = user.FindFirst("CompanyId")?.Value
                ?? throw new InvalidOperationException("Missing CompanyId claim.");
            if (!long.TryParse(companyClaim, out var companyId))
                throw new InvalidOperationException($"Invalid CompanyId claim value: '{companyClaim}'.");

            // Optional BranchId
            long? branchId = null;
            var branchClaim = user.FindFirst("CompanyBranchId")?.Value;
            if (!string.IsNullOrEmpty(branchClaim))
            {
                if (long.TryParse(branchClaim, out var bid))
                    branchId = bid;
                else
                    throw new InvalidOperationException($"Invalid CompanyBranchId claim value: '{branchClaim}'.");
            }

            // Roles → CheckBoxListItemDto
            var rolesClaim = user.FindFirst("Roles")?.Value ?? "";
            var rolesList = rolesClaim
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => new CheckBoxListItemDto { Id = r, Name = r, IsChecked = true })
                .ToList();

            return new UserViewModel
            {
                UserId = userId,
                Email = email,
                FullName = fullName,
                CompanyId = companyId,           // now non-nullable
                CompanyBranchId = branchId,
                Roles = rolesList
            };
        }

        public async Task<UserViewModel> GetStaffByEmail(string email)
        {
            UserViewModel model = null;
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var roles = _roleManager.Roles.AsNoTracking();
                model = new()
                {
                    Id = user.Id,
                    Email = email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Phone = user.PhoneNumber,
                    IsFirstLogin = user.IsFirstLogin,
                    EmailConfirmed = user.EmailConfirmed,
                    IsActive = user.IsActive,
                };
            }
            return model;
        }

        public async Task<UserViewModel> GetStaffById(string id)
        {
            UserViewModel model = null;
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                var userRole = userRoles.FirstOrDefault();
                var roles = GetRoles();// _roleManager.Roles.AsNoTracking();
                var role = roles.FirstOrDefault(x => x.Name == userRole);
                model = new()
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Phone = user.PhoneNumber,
                    IsFirstLogin = user.IsFirstLogin,
                    //DefaultRole = role.Name,
                    EmailConfirmed = user.EmailConfirmed,
                    IsActive = user.IsActive
                };
            }
            return model;
        }

        public List<RoleViewModel> GetRoles()
        {
            IEnumerable<SelectListItem> list = EnumExtensions.GetEnumSelectListWithDescriptions<Role>();
            return list.Select(x => new RoleViewModel
            {
                Id = x.Value,
                Name = x.Text
            }).OrderBy(x => x.Name).ToList();
        }

        public async Task<CompanyViewModel> GetCompanyProfile()
        {
            var userData = GetUserData(); // retrieves the logged-in user’s data from the cookie or context
            if (userData == null || userData.Id == null)
            {
                return null;
            }

            var user = await _userManager.FindByIdAsync(userData.Id);
            if (user == null || user.CompanyId == null)
            {
                return null;
            }

            var company = await _context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == user.CompanyId);

            if (company == null)
            {
                return null;
            }

            return new CompanyViewModel
            {
                Id=company.Id,
                Name = company.Name,
                RegistrationNumber = company.RegistrationNumber,
                Address = company.Address,
                DateOfIncorporation = company.DateOfIncorporation,
                State = company.State,
                Email = company.Email,
                PhoneNumber = company.PhoneNumber,
                ContactPersonName = company.ContactPersonName,
                ContactPersonPhone = company.ContactPersonPhone,
                ContactPersonEmail = company.ContactPersonEmail,
                Website = company.Website,
                LogoUrl = company.LogoUrl,
                IsVerified = company.IsVerified
            };
        }

        public async Task<MessageResponse> EditCompanyProfile(EditCompanyViewModel model)
        {
            var response = new MessageResponse();

            try
            {
                var userData = GetUserData(); 
                var user = await _userManager.FindByIdAsync(userData.Id);
                if (user == null || user.CompanyId == null)
                {
                    response.Message = "User or associated company not found.";
                    return response;
                }

                var company = await _context.Companies.FindAsync(user.CompanyId);
                if (company == null)
                {
                    response.Message = "Company not found.";
                    return response;
                }


                company.RegistrationNumber = model.RegistrationNumber;
                company.Address = model.Address;
                company.DateOfIncorporation = model.DateOfIncorporation;
                //company.State = model.State;
                company.Email = model.Email;
                company.PhoneNumber = model.PhoneNumber;
                company.ContactPersonName = model.ContactPersonName;
                company.ContactPersonPhone = model.ContactPersonPhone;
                company.ContactPersonEmail = model.ContactPersonEmail;
                company.Website = model.Website;
                company.LogoUrl = model.LogoUrl;

                if (model.StateId.HasValue)
                {
                    var selectedState = await _context.States.FindAsync(model.StateId.Value);
                    company.State = selectedState?.Name;
                }


                _context.Companies.Update(company);
                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "Company profile updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company profile.");
                response.Message = "An error occurred while updating the company profile.";
            }

            return response;
        }

        public async Task<List<StateDto>> GetAllStatesAsync()
        {
            return await _context.States
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .Select(s => new StateDto
                {
                    Id = s.Id,
                    Name = s.Name
                })
                .ToListAsync();
        }

        public async Task<List<LgaDto>> GetLgasByStateIdAsync(long stateId)
        {
            return await _context.LGAs
                .AsNoTracking()
                .Where(l => l.StateId == stateId)
                .OrderBy(l => l.Name)
                .Select(l => new LgaDto
                {
                    Id = l.Id,
                    Name = l.Name
                })
                .ToListAsync();
        }

    }
}
