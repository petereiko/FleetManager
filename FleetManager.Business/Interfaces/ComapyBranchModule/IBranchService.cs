using FleetManager.Business.DataObjects;
using FleetManager.Business.UtilityModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.ComapyBranchModule
{
    public interface IBranchService
    {
        Task<IEnumerable<CompanyBranchDto>> GetBranchesForCompanyAsync();
        Task<CompanyBranchDto?> GetBranchByIdAsync(long id);
        Task<MessageResponse> AddBranchAsync(CompanyBranchDto dto);
        Task<MessageResponse> UpdateBranchAsync(CompanyBranchDto dto);
        Task<bool> DeleteBranchAsync(long id);
    }

}
