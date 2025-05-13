using FleetManager.Business.UtilityModels;
using FleetManager.Business.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.CompanyModule
{
    public interface ICompanyService
    {
        Task<MessageResponse> OnboardCompany(CompanyRegistrationViewModel model);
        Task<MessageResponse> ConfirmEmail(string encodedToken, string userId);
    }
}
