using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;
using FleetManager.Business.Database.IdentityModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace FleetManager.App
{
    public class CustomClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public CustomClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
        {
            _userManager = userManager;
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            IList<string> userRoles = await _userManager.GetRolesAsync(user);
            string commaSeparatedRoles = string.Join(",", userRoles);

            var identity = await base.GenerateClaimsAsync(user);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
            identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
            identity.AddClaim(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber ?? ""));
            identity.AddClaim(new Claim(ClaimTypes.Name, user.UserName));
            identity.AddClaim(new Claim(ClaimTypes.Actor, $"{user.LastName} {user.FirstName}"));
            identity.AddClaim(new Claim(ClaimTypes.Role, commaSeparatedRoles));
            identity.AddClaim(new Claim(ClaimTypes.Country, user.CompanyId?.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Locality, user.CompanyBranchId?.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.PrimarySid, user.FirstName));
            return identity;
        }

    }
}
