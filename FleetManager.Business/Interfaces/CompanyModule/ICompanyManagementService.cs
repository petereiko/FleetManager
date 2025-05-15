using FleetManager.Business.Database.Entities;
using FleetManager.Business.DataObjects;
using FleetManager.Business.UtilityModels;
using FleetManager.Business.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.CompanyModule
{
    public interface ICompanyManagementService
    {
        Task<MessageResponse> OnboardCompany(CompanyRegistrationViewModel model);
        Task<MessageResponse> ConfirmEmail(string encodedToken, string userId);
        UserViewModel GetUserData();
        Task<UserViewModel> GetStaffByEmail(string email);
        Task<UserViewModel> GetStaffById(string id);
        Task<CompanyViewModel> GetCompanyProfile();
        Task<MessageResponse> EditCompanyProfile(EditCompanyViewModel model);
        Task<List<StateDto>> GetAllStatesAsync();
        Task<List<LgaDto>> GetLgasByStateIdAsync(long stateId);

    }
}
