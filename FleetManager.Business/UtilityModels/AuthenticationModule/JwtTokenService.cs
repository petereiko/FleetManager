using FleetManager.Business.Database.IdentityModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.UtilityModels.AuthenticationModule
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;

        public JwtTokenService(IConfiguration configuration, UserManager<ApplicationUser> userManager)
        {
            _configuration = configuration;
            _userManager = userManager;
        }

        //public async Task<string> GenerateTokenAsync(ApplicationUser user)
        //{
        //    var userRoles = await _userManager.GetRolesAsync(user);

        //    var claims = new List<Claim>
        //{
        //    new Claim(ClaimTypes.NameIdentifier, user.Id),
        //    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
        //    new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
        //    new Claim(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty),
        //    new Claim("FullName", $"{user.FirstName} {user.LastName}"),
        //    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        //};

        //    // Add roles as claims
        //    foreach (var role in userRoles)
        //    {
        //        claims.Add(new Claim(ClaimTypes.Role, role));
        //    }

        //    // Add CompanyId and CompanyBranchId if available
        //    if (user.CompanyId.HasValue)
        //    {
        //        claims.Add(new Claim("CompanyId", user.CompanyId.Value.ToString()));
        //    }

        //    if (user.CompanyBranchId.HasValue)
        //    {
        //        claims.Add(new Claim("CompanyBranchId", user.CompanyBranchId.Value.ToString()));
        //    }

        //    var key = new SymmetricSecurityKey(
        //        Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not configured"))
        //    );

        //    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        //    var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryInMinutes"] ?? "1440");

        //    var token = new JwtSecurityToken(
        //        issuer: _configuration["JwtSettings:Issuer"],
        //        audience: _configuration["JwtSettings:Audience"],
        //        claims: claims,
        //        expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
        //        signingCredentials: credentials
        //    );

        //    return new JwtSecurityTokenHandler().WriteToken(token);
        //}

        public async Task<string> GenerateTokenAsync(ApplicationUser user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
    {
        // Core identity
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
        new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
        new Claim(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty),

        // Full name (AuthUser expects Actor)
        new Claim(ClaimTypes.Actor, $"{user.FirstName} {user.LastName}"),

        // First name (AuthUser expects PrimarySid)
        new Claim(ClaimTypes.PrimarySid, user.FirstName ?? string.Empty),

        // Keep your existing custom claim (non-breaking)
        new Claim("FullName", $"{user.FirstName} {user.LastName}"),

        // JWT id
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

            // Roles (AuthUser reads ClaimTypes.Role)
            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // CompanyId
            if (user.CompanyId.HasValue)
            {
                // AuthUser expects Country
                claims.Add(new Claim(ClaimTypes.Country, user.CompanyId.Value.ToString()));

                // Keep custom claim for APIs/mobile
                claims.Add(new Claim("CompanyId", user.CompanyId.Value.ToString()));
            }

            // CompanyBranchId
            if (user.CompanyBranchId.HasValue)
            {
                // AuthUser expects Locality
                claims.Add(new Claim(ClaimTypes.Locality, user.CompanyBranchId.Value.ToString()));

                // Keep custom claim for APIs/mobile
                claims.Add(new Claim("CompanyBranchId", user.CompanyBranchId.Value.ToString()));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _configuration["JwtSettings:SecretKey"]
                    ?? throw new InvalidOperationException("JWT Secret Key not configured")
                )
            );

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryInMinutes"] ?? "1440");

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public ClaimsPrincipal? ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"] ?? string.Empty);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["JwtSettings:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["JwtSettings:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}
