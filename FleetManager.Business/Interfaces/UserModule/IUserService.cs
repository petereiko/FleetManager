using FleetManager.Business.UtilityModels;
using FleetManager.Business.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.UserModule
{
    public interface IUserService
    {
        Task SeedRoles();
        Task<string> GeneratePasswordResetToken(string id);
        //UserViewModel GetUserData();
        Task<List<UserViewModel>> GetUsersInRole(string roleName);
        Task<UserViewModel> GetUserByEmail(string email);
        Task<UserViewModel> GetUserById(string id);
        Task<List<UserViewModel>> GetAllUsers();
        List<RoleViewModel> GetRoles();

        Task<MessageResponse<UserViewModel>> Authenticate(LoginViewModel model);
        Task<MessageResponse> SendResetPasswordToken(ForgotPasswordViewModel model);

        Task<MessageResponse> ChangePasswordAsync(ChangePasswordViewModel model);
        Task<MessageResponse> AdminResetPasswordAsync(string password, string userId);
        //Task<MessageResponse> OnboardFacilityManager(FacilityManagerViewModel model);
        Task<MessageResponse> OnboardUser(UserViewModel model);
        Task<MessageResponse> ResetPassword(ResetPasswordViewModel model);
        Task<MessageResponse> EditUser(UserViewModel model);
        Task<MessageResponse<UserViewModel>> LoginAsync(LoginViewModel model);
        Task<MessageResponse> Logout();
        Task<bool> ConfirmEmail(string encodedToken, string userid);
    }
}
