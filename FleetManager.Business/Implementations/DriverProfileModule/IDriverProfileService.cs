using FleetManager.Business.DataObjects;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.DriverProfileModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations
{
    public class DriverProfileService : IDriverProfileService
    {
        private readonly FleetManagerDbContext _context;
        private readonly IHostEnvironment _env;
        private readonly ILogger<DriverProfileService> _logger;
        private readonly IAuthUser _authUser;

        public DriverProfileService(
            FleetManagerDbContext context,
            IHostEnvironment env,
            ILogger<DriverProfileService> logger,
            IAuthUser authUser)
        {
            _context = context;
            _env = env;
            _logger = logger;
            _authUser = authUser;
        }

        public async Task<DriverDto?> GetProfileAsync(string userId)
        {
            var driver = await _context.Drivers
                .Include(d => d.User)
                .Include(d => d.DriverDocuments)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driver == null) return null;

            return new DriverDto
            {
                Id = driver.Id,
                FirstName = driver.User.FirstName,
                LastName = driver.User.LastName,
                FullName = $"{driver.User.FirstName} {driver.User.LastName}",
                Email = driver.User.Email,
                PhoneNumber = driver.User.PhoneNumber,
                Address = driver.Address,
                DateOfBirth = driver.DateOfBirth,
                Gender = driver.Gender,
                EmploymentStatus = driver.EmploymentStatus,
                LicenseNumber = driver.LicenseNumber,
                LicenseExpiryDate = driver.LicenseExpiryDate,
                LicenseCategory = driver.LicenseCategory,
                CompanyBranchId = driver.CompanyBranchId,
                ShiftStatus = driver.ShiftStatus,
                IsActive = driver.IsActive,
                CreatedDate = driver.CreatedDate,
                PassportFileName = _authUser.BaseUrl + "/Driverimages/Profile/" + driver.PassportFileName,
                DriverDocuments = driver.DriverDocuments
                    .Select(x => new DriverDocumentDto
                    {
                        Id = x.Id,
                        FileName = _authUser.BaseUrl + "/Driverimages/License/" + x.FileName,
                        UploadedDate = x.CreatedDate
                    }).ToList()
            };
        }

    }
}