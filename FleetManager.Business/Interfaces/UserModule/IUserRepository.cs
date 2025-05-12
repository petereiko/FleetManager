using FleetManager.Business.UtilityModels;
using FleetManager.Business.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.UserModule
{
    public interface IUserRepository
    {
        List<UserViewModel> GetUsers(string roleName, string CreatedBy);

        UserViewModel GetUserDetails(string Id);
        Task<MessageResponse> UpdateAsync(UserViewModel model);

    }
}
