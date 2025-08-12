using DocumentFormat.OpenXml.Spreadsheet;
using FleetManager.Business;
using FleetManager.Business.Interfaces.UserModule;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.UserModule
{
    public class AuthUser: IAuthUser
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly FleetManagerDbContext _context;

        public AuthUser(IHttpContextAccessor accessor, FleetManagerDbContext context)
        {
            _accessor = accessor;
            _context = context;
        }

        private ClaimsPrincipal User => _accessor.HttpContext?.User;

        //identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
        //    identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
        //    identity.AddClaim(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber ?? ""));
        //    identity.AddClaim(new Claim(ClaimTypes.Name, user.UserName));
        //    identity.AddClaim(new Claim(ClaimTypes.Actor, $"{user.LastName} {user.FirstName}"));
        //    identity.AddClaim(new Claim(ClaimTypes.Role, commaSeparatedRoles));

        public string Email => _accessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value!;
        public string UserId => _accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        public string Roles => _accessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value!;
        public string FullName => _accessor.HttpContext?.User?.FindFirst(ClaimTypes.Actor)?.Value!;
        public string FirstName => _accessor.HttpContext?.User?.FindFirst(ClaimTypes.PrimarySid)?.Value!;

        public long? CompanyId
        {
            get
            {
                var companyClaim = User?.FindFirst(ClaimTypes.Country)?.Value;
                return long.TryParse(companyClaim, out var id) ? id : null;
            }
        }

        public long? CompanyBranchId
        {
            get
            {
                var branchClaim = User?.FindFirst(ClaimTypes.Locality)?.Value;
                return long.TryParse(branchClaim, out var id) ? id : null;
            }
        }

        public long? VendorId
        {
            get
            {
                var vendorClaim = User?.FindFirst("VendorId")?.Value;
                return long.TryParse(vendorClaim, out var id) ? id : null;
            }
        }


        public string BaseUrl
        {
            get
            {
                return $"{_accessor.HttpContext.Request.Scheme}://{_accessor.HttpContext.Request.Host}";
            }
        }
    }
}
