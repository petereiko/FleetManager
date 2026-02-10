using FleetManager.Business.Database.IdentityModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.UtilityModels.AuthenticationModule
{
    public interface IJwtTokenService
    {
        Task<string> GenerateTokenAsync(ApplicationUser user);
        ClaimsPrincipal? ValidateToken(string token);
    }
}
