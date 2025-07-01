using FleetManager.Business.DataObjects;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.DriverProfileModule
{
    public interface IDriverProfileService
    {
        Task<DriverDto?> GetProfileAsync(string userId);
        //Task<MessageResponse> UpdateProfileAsync(DriverDto dto, string modifiedBy);
        //Task<MessageResponse> UploadProfilePhotoAsync(long driverId, IFormFile photo, string uploadedBy);
        //Task<MessageResponse> UploadLicensePhotoAsync(long driverId, IFormFile photo, string uploadedBy);
    }

}
