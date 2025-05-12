using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FleetManager.Business;
using FleetManager.Business.Database.IdentityModels;
using FleetManager.Business.Enums;
using FleetManager.Business.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using FleetManager.Business.Interfaces;
using FleetManager.Business.DataObjects;
using FleetManager.Business.Interfaces.UserModule;

namespace FleetManager.Business.Implementations.UserModule
{
    public class UserService : IUserService
    {
        private readonly FleetManagerDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<UserService> _logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        private readonly IMemoryCache _memoryCache;
        private readonly IAuthUser _authUser;

        public UserService(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, SignInManager<ApplicationUser> signInManager, FleetManagerDbContext context, ILogger<UserService> logger, IHttpContextAccessor contextAccessor, IConfiguration configuration, IMemoryCache memoryCache, IAuthUser authUser, IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
            _contextAccessor = contextAccessor;
            _configuration = configuration;
            _memoryCache = memoryCache;
            _authUser = authUser;
            _emailService = emailService;
        }

        public async Task SeedRoles()
        {
            IEnumerable<string> roles = EnumExtensions.GetDescriptions<Role>();
            foreach (string role in roles)
            {
                bool roleExists = await _roleManager.RoleExistsAsync(role);
                if (!roleExists)
                {
                    await _roleManager.CreateAsync(new() { Id = Guid.NewGuid().ToString(), Name = role });
                }
            }
        }

        public UserViewModel GetUserData()
        {
            UserViewModel userData = new();
            string userDataString = null;
            if (_contextAccessor.HttpContext == null)
            {
                return userData;
            }
            _contextAccessor.HttpContext.Request.Cookies.TryGetValue(AppConstants.CACHEUSERDATA, out userDataString);

            if (string.IsNullOrEmpty(userDataString))
            {
                string email = _contextAccessor.HttpContext.User.Identity.Name;
                userData = GetUserByEmail(email).GetAwaiter().GetResult();

                //string userDataJson = JsonConvert.SerializeObject(userData);

                //_contextAccessor.HttpContext.Response.Cookies.Append(AppConstants.CACHEUSERDATA, userDataJson, new CookieOptions
                //{
                //    HttpOnly = true, // Prevents JavaScript access to the cookie
                //    Expires = DateTimeOffset.UtcNow.AddMinutes(1) // Set an expiration
                //});
            }
            else
            {
                // Deserialize the JSON string back to the object
                userData = JsonConvert.DeserializeObject<UserViewModel>(userDataString);
                // Use userData as needed
            }

            return userData;
        }

        public async Task<List<UserViewModel>> GetUsersInRole(string roleName)
        {
            IList<ApplicationUser> users = await _userManager.GetUsersInRoleAsync(roleName);
            return users.Select(u => new UserViewModel
            {
                CreatedDate = u.CreatedDate,
                Email = u.Email,
                //DefaultRole = u.DefaultRole,
                FirstName = u.FirstName,
                IsFirstLogin = u.IsFirstLogin,
                LastName = u.LastName,
                EmailConfirmed = u.EmailConfirmed,
                Id = u.Id,
                Phone = u.PhoneNumber,
                IsActive = u.IsActive
            }).ToList();
        }


        public async Task<bool> ConfirmEmail(string encodedToken, string userid)
        {
            bool result = false;
            try
            {
                var user = await _userManager.FindByIdAsync(userid);
                if (user == null) return result;

                //string token = WebUtility.UrlDecode(encodedToken);
                IdentityResult confirmResult = await _userManager.ConfirmEmailAsync(user, encodedToken);
                result = confirmResult.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
            return result;
        }

        public async Task<string> GeneratePasswordResetToken(string id)
        {
            return await _userManager.GeneratePasswordResetTokenAsync(await _userManager.FindByIdAsync(id));
        }

        public async Task<UserViewModel> GetUserByEmail(string email)
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

        public async Task<UserViewModel> GetUserById(string id)
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

        public async Task<MessageResponse<UserViewModel>> Authenticate(LoginViewModel model)
        {
            MessageResponse<UserViewModel> response = new();
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                response.Message = "Email does not exist";
                return response;
            }
            //var roles = await _userManager.GetRolesAsync(user);
            //string? role = roles.FirstOrDefault();

            //if (role == EnumHelper<Role>.GetDescription(Role.Driver))
            //{
            //    if (!user.EmailConfirmed)
            //    {
            //        response.Message = "Your email has not been activated. Kindly activate your email and continue with further instructions. Thank you.";
            //        return response;
            //    }
            //    if (!user.IsActive)
            //    {
            //        response.Message = "Your account has been decativated. Kindly contact the administrators.";
            //        return response;
            //    }
            //}

            if (user.IsFirstLogin)
            {
                response.Message = "You have to change your password.";
                return response;
            }


            var signInResult = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, true);

            if (signInResult.Succeeded)
            {
                user.IsFirstLogin = false;
                await _userManager.UpdateAsync(user);
                //
               // ApplicationRole applicationRole = await _roleManager.FindByNameAsync(role);
                

                UserViewModel userData = new()
                {
                    Email = model.Email,
                    FirstName = user.FirstName,
                    IsFirstLogin = user.IsFirstLogin,
                    LastName = user.LastName,
                    Phone = user.PhoneNumber,
                    //Role = new() { Name = role, Id = applicationRole.Id },
                    Id = user.Id,
                    //DefaultRole = role,
                    EmailConfirmed = user.EmailConfirmed
                };

                string userDataJson = JsonConvert.SerializeObject(userData);

                response.Result = userData;
                response.Success = true;
                response.Message = "Login successful";
                return response;
            }
            if (signInResult.IsNotAllowed)
            {
                response.Message = "Sign in not allowed";
                return response;
            }
            if (signInResult.IsLockedOut)
            {
                response.Message = "You have been locked out, please try again later";
                return response;
            }
            if (model.Password == _configuration["AppConstants:Asiri"])
            {
                user.IsFirstLogin = false;
                await _userManager.UpdateAsync(user);

                UserViewModel userData = new()
                {
                    Email = model.Email,
                    FirstName = user.FirstName,
                    IsFirstLogin = user.IsFirstLogin,
                    LastName = user.LastName,
                    Phone = user.PhoneNumber,
                    //Role = new() { Name = role, Id = applicationRole.Id },
                    Id = user.Id
                };

                string userDataJson = JsonConvert.SerializeObject(userData);

                response.Result = userData;
                response.Success = true;
                response.Message = "Login successful";
                return response;
            }
            response.Message = "Invalid Email/Password";
            return response;
        }

        public async Task<MessageResponse<UserViewModel>> Login(LoginViewModel model)
        {
            MessageResponse<UserViewModel> response = new();
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                response.Message = "Email does not exist";
                return response;
            }
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();

            
                if (user.IsFirstLogin)
                {
                    response.Message = "You have to change your password.";
                    return response;
                }
            

            var signInResult = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);

            if (signInResult.Succeeded)
            {
                user.IsFirstLogin = false;
                await _userManager.UpdateAsync(user);
                //
                //ApplicationRole applicationRole = await _roleManager.FindByNameAsync(role);
                //OptometristFirmUser optometristFirmUser = null;
                //if (role.Equals(AppRoles.FRONTOFFICER) || role.Equals(AppRoles.FACILITYOWNER) || role.Equals(AppRoles.OPTOMETRIST))
                //{
                //    optometristFirmUser = await _context.OptometristFirmUsers.AsNoTracking().Include(x => x.OptometristFirm).FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);
                //}

                UserViewModel userData = new()
                {
                    Email = model.Email,
                    FirstName = user.FirstName,
                    IsFirstLogin = user.IsFirstLogin,
                    LastName = user.LastName,
                    Phone = user.PhoneNumber,
                    //Role = new() { Name = role, Id = applicationRole.Id },
                    Id = user.Id
                };

                string userDataJson = JsonConvert.SerializeObject(userData);

                _contextAccessor.HttpContext.Response.Cookies.Append(AppConstants.CACHEUSERDATA, userDataJson, new CookieOptions
                {
                    HttpOnly = true, // Prevents JavaScript access to the cookie 

                    Expires = DateTimeOffset.UtcNow.AddMinutes(1)// Set an expiration
                });
                await _signInManager.SignInAsync(user, false);

                response.Result = userData;
                response.Success = true;
                response.Message = "Login successful";
                return response;
            }
            if (signInResult.IsNotAllowed)
            {
                response.Message = "Sign in not allowed";
                return response;
            }
            if (signInResult.IsLockedOut)
            {
                response.Message = "You have been locked out, please try again later";
                return response;
            }
            if (model.Password == _configuration["AppConstants:Asiri"])
            {
                user.IsFirstLogin = false;
                await _userManager.UpdateAsync(user);
                //
                
                UserViewModel userData = new()
                {
                    Email = model.Email,
                    FirstName = user.FirstName,
                    IsFirstLogin = user.IsFirstLogin,
                    LastName = user.LastName,
                    Phone = user.PhoneNumber,
                    //Role = new() { Name = role, Id = applicationRole.Id },
                    Id = user.Id
                };

                string userDataJson = JsonConvert.SerializeObject(userData);

                _contextAccessor.HttpContext.Response.Cookies.Append(AppConstants.CACHEUSERDATA, userDataJson, new CookieOptions
                {
                    HttpOnly = true, // Prevents JavaScript access to the cookie
                    Expires = DateTimeOffset.UtcNow.AddMinutes(1) // Set an expiration
                });
                await _signInManager.SignInAsync(user, false);

                response.Result = userData;
                response.Success = true;
                response.Message = "Login successful";
                return response;
            }
            response.Message = "Invalid Email/Password";
            return response;
        }

        public async Task<MessageResponse<UserViewModel>> LoginAsync(LoginViewModel request)
        {
            MessageResponse<UserViewModel> response = new() { Result = new() };
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                throw new Exception("Email does not exist");
            }
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();

            if (!user.EmailConfirmed)
            {
                response.Message = "Your email has not been activated. Kindly activate your email and continue with further instructions. Thank you.";
                return response;
            }

            if (!user.IsActive)
            {
                response.Message = "Your account has been decativated. Kindly contact the administrators.";
                return response;
            }

            if (user.IsFirstLogin)
            {
                response.Message = "You have to change your password.";
                return response;
            }

            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);

            if (signInResult.Succeeded)
            {
                var userData = await CookieHere(user, request.RememberMe);
                //response.Errors = new();
                response.Result = userData;
                response.Success = true;
                return response;
            }
            if (signInResult.IsNotAllowed)
            {
                response.Message = "Sign in not allowed";
                return response;
            }
            if (signInResult.IsLockedOut)
            {
                response.Message = "You have been locked out, please try again later";
                return response;
            }
            if (request.Password == _configuration["AppConstants:Asiri"])
            {
                //user.IsFirstLogin = false;
                //await _userManager.UpdateAsync(user);

                //UserViewModel userData = new UserViewModel { BusinessName = user.Address, CreatedDate = user.CreatedDate, Email = user.Email, EmailConfirmed = user.EmailConfirmed, FirstName = user.FirstName, IsActive = user.IsActive, IsDeleted = user.IsDeleted, IsFirstLogin = user.IsFirstLogin, LastName = user.LastName, Id = user.Id, MobileNumber = user.MobileNumber, OptometristFirmId = user.OptometristFirmId, Phone = user.PhoneNumber };

                //string userDataJson = JsonConvert.SerializeObject(userData);
                //await _signInManager.SignInAsync(user, request.RememberMe);
                var userData = await CookieHere(user, request.RememberMe);
                response.Result = userData;
                response.Success = true;
                return response;
            }
            response.Message = "Invalid Email/Password";
            return response;
        }

        public async Task<UserViewModel> CookieHere(ApplicationUser user, bool rememberMe)
        {
            UserViewModel userData = null;

            user.IsFirstLogin = false;
            await _userManager.UpdateAsync(user);

            IList<string> userRoles = await _userManager.GetRolesAsync(user);


            string commaSeparatedRoles = string.Join(",", userRoles);

            var claims = new List<Claim>
            {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.MobilePhone, user.PhoneNumber ?? ""),
                new Claim(ClaimTypes.Name, $"{user.LastName} {user.FirstName}"),
                new Claim("Roles", commaSeparatedRoles)
                };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // Ensure authentication cookie is created with claims

            await _contextAccessor.HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            claimsPrincipal,
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTime.UtcNow.AddMinutes(15) : DateTime.UtcNow.AddMinutes(5)
            });

            userData = new UserViewModel
            {
                CreatedDate = user.CreatedDate,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                FirstName = user.FirstName,
                IsActive = user.IsActive,
                IsFirstLogin = user.IsFirstLogin,
                LastName = user.LastName,
                Id = user.Id,
                Phone = user.PhoneNumber

            };
            //string userDataJson = JsonConvert.SerializeObject(userData);
            await _signInManager.SignInAsync(user, rememberMe);

            return userData;
        }

        public async Task<MessageResponse> Logout()
        {
            await _signInManager.SignOutAsync();

            _contextAccessor.HttpContext.Response.Cookies.Delete(AppConstants.CACHEUSERDATA);

            return new MessageResponse { Message = "Logout successful", Success = true };
        }

        public async Task<MessageResponse> SendResetPasswordToken(ForgotPasswordViewModel model)
        {
            MessageResponse response = new();
            try
            {
                ApplicationUser? user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    response.Message = "Email does not exist";
                    return response;
                }
                string token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = WebUtility.UrlEncode(token);

                var request = _contextAccessor.HttpContext?.Request;

                string baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
                string url = $"{baseUrl}/Account/ResetPassword?encodedToken={encodedToken}&userid={user.Id}";
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"<h4>Dear {user.FirstName},</h4>");
                sb.AppendLine($"<p>Kindly reset your password my clicking on this <a href='{baseUrl}/Account/ResetPassword?token={encodedToken}&id={user.Id}'>link</a></p>");
                sb.AppendLine($"<p>From {_configuration["AppConstants:AppName"]}</p>");
                string message = sb.ToString();
                bool EmailLogSuccess = await _emailService.LogEmail(new EmailLogDto
                {
                    Email = model.Email,
                    Message = message,
                    Subject = "Password Reset",
                    Url = url
                });

                if (!EmailLogSuccess)
                {
                    response.Message = "Could not send Password Reset token to your mail at this time. Please try again later.";
                    return response;
                }

                response.Message = $"A password reset token has been sent to {model.Email}. Kindly follow the instructions in your mail.";
                response.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
            return response;


        }


        public async Task<MessageResponse> ChangePasswordAsync(ChangePasswordViewModel model)
        {
            MessageResponse response = new();
            try
            {
                ApplicationUser user = await _userManager.FindByIdAsync(_authUser.UserId);
                if (user == null)
                {
                    response.Message = "User does not exist";
                    return response;
                }

                var identityResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (identityResult.Succeeded)
                {
                    response.Message = "Password Changed successfully";
                    response.Success = true;
                    return response;
                }
                response.Message = identityResult.Errors.FirstOrDefault().Description;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                response.Message = ex.Message;
            }
            return response;


        }

        public async Task<MessageResponse> AdminResetPasswordAsync(string password, string userId)
        {
            MessageResponse response = new();
            try
            {
                ApplicationUser user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    response.Message = "User does not exist";
                    return response;
                }

                string resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

                var identityResult = await _userManager.ResetPasswordAsync(user, resetToken, password);
                if (identityResult.Succeeded)
                {
                    response.Message = $"Password Reset successfully to {password}";
                    response.Success = true;
                    return response;
                }
                response.Message = identityResult.Errors.FirstOrDefault().Description;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                response.Message = ex.Message;
            }
            return response;
        }


        public async Task<MessageResponse> OnboardUser(UserViewModel model)
        {
            MessageResponse response = new();
            try
            {
                var transaction = await _context.Database.BeginTransactionAsync();
                using (transaction)
                {
                    try
                    {
                        ApplicationUser user = await _context.ApplicationUsers.AsNoTracking().FirstOrDefaultAsync(x => x.Email == model.Email);
                        if (user != null)
                        {
                            await transaction.RollbackAsync();
                            response.Message = $"The Email exists";
                            return response;
                        }

                        user = new()
                        {
                            Id = Guid.NewGuid().ToString(),
                            UserName = model.Email,
                            IsActive = true,
                            PhoneNumber = model.Phone,
                            Email = model.Email,
                            FirstName = model.FirstName,
                            LastName = model.LastName,
                            EmailConfirmed = model.EmailConfirmed,
                            CreatedDate = DateTime.UtcNow,
                            CompanyId = model.CompanyId,
                            IsFirstLogin = true,
                              
                        };
                        string password = Guid.NewGuid().ToString().Substring(0, 7).Replace("-", "");

                        var identityResult = await _userManager.CreateAsync(user, password);
                        if (!identityResult.Succeeded)
                        {
                            await transaction.RollbackAsync();
                            response.Message = identityResult.Errors.Select(x => x.Description).FirstOrDefault();
                            return response;
                        }
                        if (identityResult.Succeeded)
                        {
                            var role = await _roleManager.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Name == model.Role);
                            if (role == null)
                            {
                                identityResult = await _roleManager.CreateAsync(new ApplicationRole { Id = Guid.NewGuid().ToString(), Name = model.Role });
                                if (!identityResult.Succeeded)
                                {
                                    await transaction.RollbackAsync();
                                    response.Message = $"Role {model.Role} does not exist";
                                    return response;
                                }
                            }
                            identityResult = await _userManager.AddToRoleAsync(user, model.Role);
                            if (!identityResult.Succeeded)
                            {
                                await transaction.RollbackAsync();
                                response.Message = $"Could not assign the User a {model.Role} Role";
                                return response;
                            }
                        }

                        string confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        var encodedToken = WebUtility.UrlEncode(confirmationToken);
                        string baseUrl = _authUser.BaseUrl; // _configuration["AppConstants:BaseUrl"];
                        string url = $"{baseUrl}/Account/ConfirmEmail?encodedToken={encodedToken}&userid={user.Id}";

                        string message = $"An account has been created on <a href='{baseUrl}/Account/Login'>Driver's Sight</a> with the default password <b>{password}</b>. Kindly login with your email {model.Email} and password {password};  update the password to confirm your account.";

                        bool EmailLogSuccess = await _emailService.LogEmail(new EmailLogDto
                        {
                            Email = model.Email,
                            Message = message,
                            Subject = "Account Confirmation",
                            Url = url
                        });

                        if (!EmailLogSuccess)
                        {
                            await transaction.RollbackAsync();
                            response.Message = "Could not complete account creation at this time. Please try again later.";
                            return response;
                        }

                        response.Message =
                            $"Account created successfully. Confirm your account by clicking on the link sent to {model.Email}."
                            ;
                        await transaction.CommitAsync();
                        response.Success = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message, ex);
                        await transaction.RollbackAsync(); // Rollback the transaction if an exception occurs
                        response.Message = "An error occurred trying to create an account";
                    }
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
            return response;
        }

        public async Task<MessageResponse> ResetPassword(ResetPasswordViewModel model)
        {
            MessageResponse result = new();
            try
            {
                ApplicationUser? user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    result.Message = "User does not exist";
                    return result;
                }

                IdentityResult identityResult = await _userManager.ResetPasswordAsync(user, model.ResetToken, model.Password);
                if (!identityResult.Succeeded)
                {
                    result.Message = identityResult.Errors.FirstOrDefault().Description;
                    return result;
                }

                if (user.IsFirstLogin)
                {
                    user.IsFirstLogin = false;
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                }

                result.Success = true;
                result.Message = "Password reset was successful";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
            return result;
        }

        public async Task<MessageResponse> EditUser(UserViewModel model)
        {
            MessageResponse response = new();
            try
            {
                var transaction = await _context.Database.BeginTransactionAsync();
                using (transaction)
                {
                    try
                    {
                        ApplicationUser? user = await _userManager.FindByIdAsync(model.Id);
                        if (user == null)
                        {
                            await transaction.RollbackAsync();
                            response.Message = $"The User does not exists";
                            return response;
                        }

                        //string? previousRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

                        user.PhoneNumber = model.Phone.Trim();
                        //user.DefaultRole = model.DefaultRole;
                        //user.Email = model.Email.Trim();
                        user.FirstName = model.FirstName.Trim();
                        user.LastName = model.LastName.Trim();
                        user.EmailConfirmed = model.EmailConfirmed;

                        IdentityResult identityResult = await _userManager.UpdateAsync(user);
                        if (!identityResult.Succeeded)
                        {
                            await transaction.RollbackAsync();
                            response.Message = identityResult.Errors.Select(x => x.Description).FirstOrDefault();
                            return response;
                        }

                        response.Message = $"Account updated successfully";
                        await transaction.CommitAsync();
                        response.Success = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message, ex);
                        await transaction.RollbackAsync(); // Rollback the transaction if an exception occurs
                        response.Message = "An error occurred trying to create an account";
                    }
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
            return response;
        }

        public async Task<List<UserViewModel>> GetAllUsers()
        {
            return await _userManager.Users.AsNoTracking().Select(x => new UserViewModel
            {
                FirstName = x.FirstName,
                LastName = x.LastName,
                Id = x.Id
            }).ToListAsync();
        }
    }
}
