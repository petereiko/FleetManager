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

        public string Email => _accessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value!;
        public string UserId => _accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        public string Roles => _accessor.HttpContext?.User?.FindFirst("Roles")?.Value!;
        public string FullName => _accessor.HttpContext?.User?.FindFirst("FullName")?.Value!;
        public int? OptometristFirmId
        {
            get
            {
                int? optoId = null;
                int optometristFirmId;
                string claim = _accessor.HttpContext?.User?.FindFirst("OptometristFirmId")?.Value!;
                bool result = int.TryParse(claim, out optometristFirmId);
                optoId = result ? optometristFirmId > 0 ? optometristFirmId : null : null;

                return optoId;
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
