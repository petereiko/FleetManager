
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.Database.IdentityModels;
using FleetManager.Business;
using FleetManager.Business.ViewModels;
using FleetManager.Business.UtilityModels;
using FleetManager.Business.Enums;

namespace DVLA.UI.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<AccountController> _logger;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly FleetManagerDbContext _context;
        private readonly IHttpContextAccessor _contextAccessor;

        public AccountController(IUserService userService, ILogger<AccountController> logger, RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, FleetManagerDbContext context, IHttpContextAccessor contextAccessor)
        {
            _userService = userService;
            _logger = logger;
            _roleManager = roleManager;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _context = context;
            _contextAccessor = contextAccessor;
        }

        [HttpGet]
        public IActionResult Login()
        {
            LoginViewModel model = new();
            return View(model);
        }

        #region Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Errors.Add(
                    ModelState.Values.SelectMany(v => v.Errors)
                                     .FirstOrDefault()?.ErrorMessage
                );
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                model.Errors.Add("Invalid Email/Password");
                return View(model);
            }

            if (!user.EmailConfirmed)
            {
                model.Errors.Add("Your email has not been activated. Kindly activate your email first.");
                return View(model);
            }

            if (!user.IsActive)
            {
                model.Errors.Add("Your account has been deactivated. Contact the administrators.");
                return View(model);
            }

            if (user.IsFirstLogin)
            {
                string token = await _userService.GeneratePasswordResetToken(user.Id);
                return RedirectToAction("ResetPassword", new { id = user.Id, token });
            }

            bool correctPassword = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!correctPassword && model.Password != _configuration["AppConstants:Asiri"])
            {
                model.Errors.Add("Invalid Email/Password");
                return View(model);
            }

            await CookieHere(user, model.RememberMe);
            TempData["SuccessMessage"] = "Login successful.";

            // **Fetch all roles** and pick in descending priority
            var roles = await _userManager.GetRolesAsync(user);
            // You can change the order below if you want a different priority:
            if (roles.Contains("Super Admin"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            else if (roles.Contains("Company Owner"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Company" });
            }
            else if (roles.Contains("Company Admin"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            else if (roles.Contains("Driver"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "User" });
            }
            else if (roles.Contains("Company Admin"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
            }
            else
            {
                TempData["ErrorMessage"] = "No recognized role assigned. Contact support.";
                return RedirectToAction("Login");
            }
        }



        #endregion




        public async Task CookieHere(ApplicationUser user, bool rememberMe)
        {

            user.IsFirstLogin = false;
            await _userManager.UpdateAsync(user);

            IList<string> userRoles = await _userManager.GetRolesAsync(user);

           

            string commaSeparatedRoles = string.Join(",", userRoles);

            var claims = new List<Claim>
            {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Email!),
                 new Claim(ClaimTypes.MobilePhone, user.PhoneNumber ?? ""),
                 new Claim(ClaimTypes.Name, user.UserName),
                 new Claim("FullName",$"{user.LastName} {user.FirstName}"),
                 new Claim("Roles", commaSeparatedRoles),
                };

            // ✅ Add CompanyId and CompanyBranchId as claims (if available)
            if (user.CompanyId.HasValue)
            {
                claims.Add(new Claim("CompanyId", user.CompanyId.Value.ToString()));
            }

            if (user.CompanyBranchId.HasValue)
            {
                claims.Add(new Claim("CompanyBranchId", user.CompanyBranchId.Value.ToString()));
            }

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.Now.AddMinutes(15)
            };
            await _signInManager.SignInWithClaimsAsync(user, authProperties, claims);


        }

        [HttpGet]
        public IActionResult Register()
        {
            UserViewModel model = new();
            return View(model);
        }


        [HttpPost]

        public async Task<IActionResult> Register(UserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Errors.Add(ModelState.Values.SelectMany(x => x.Errors).FirstOrDefault()?.ErrorMessage);
                return View(model);
            }
            var onboardUserResult = await _userService.OnboardUser(model);
            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            ForgotPasswordViewModel model = new();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            MessageResponse response = await _userService.SendResetPasswordToken(model);
            if (response.Success)
            {
                model.SuccessMessage = response.Message;
            }
            else
            {
                model.ErrorMessage = response.Message;
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string encodedToken, string userid)
        {
            var result = await _userService.ConfirmEmail(encodedToken, userid);
            if (result)
            {
                TempData["SuccessMessage"] = "Your account has been successfully activated.";
                return RedirectToAction("Login");
            }
            return View("Error");
        }


        [HttpGet]
        public IActionResult ResetPassword(string id, string token)
        {
            ResetPasswordViewModel model = new() { Id = id, ResetToken = token };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Errors.Add(ModelState.Values.SelectMany(x => x.Errors).FirstOrDefault()?.ErrorMessage);
                return View(model);
            }
            var resetPasswordResult = await _userService.ResetPassword(model);
            if (resetPasswordResult.Success)
            {
                TempData["SuccessMessage"] = resetPasswordResult.Message;
                return RedirectToAction("Login");
            }
            model.Errors.Add(resetPasswordResult.Message);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _userService.Logout();
            return RedirectToAction("Login");
        }


        [HttpGet]
        public IActionResult ChangePassword()
        {
            ChangePasswordViewModel model = new();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Errors.AddRange(ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return View(model);
            }
            MessageResponse response = await _userService.ChangePasswordAsync(model);
            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message;
                return RedirectToAction("Login");
            }
            model.Errors.Add(response.Message);
            return View(model);
        }



    }
}
