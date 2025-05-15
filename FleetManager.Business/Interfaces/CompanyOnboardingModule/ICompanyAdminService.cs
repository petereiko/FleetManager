using FleetManager.Business.DataObjects;
using FleetManager.Business.UtilityModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.CompanyOnboardingModule
{
    public interface ICompanyAdminService
    {
        Task<MessageResponse> OnboardCompanyAdminAsync(CompanyAdminOnboardingDto dto, string createdByUserId);
        Task<IEnumerable<CompanyAdminDto>> GetAllAdminsAsync();
    }

}
