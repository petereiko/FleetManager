using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using FleetManager.Business;
using Microsoft.Extensions.Logging;
using FleetManager.Business.Database.IdentityModels;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.ViewModels;
using FleetManager.Business.UtilityModels;
namespace DVLA.Business.UserModule
{
    public class UserRepository : IUserRepository, IDisposable
    {
        private readonly FleetManagerDbContext _context;
        private readonly ILogger<UserRepository> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IUserService _userService;
        private readonly IAuthUser _authUser;
        public UserRepository(FleetManagerDbContext context, ILogger<UserRepository> logger, IConfiguration configuration, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, IUserService userService, IAuthUser authUser)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _userManager = userManager;
            _roleManager = roleManager;
            _userService = userService;
            _authUser = authUser;
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public UserViewModel GetUserDetails(string Id)
        {
            UserViewModel user = new();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("FetchUserById", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserId", Id ?? System.Data.SqlTypes.SqlString.Null);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            user = new()
                            {
                                Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                                FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? null : reader.GetString(reader.GetOrdinal("FirstName")),
                                IsActive = reader.IsDBNull(reader.GetOrdinal("IsActive")) ? false : reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                Id = reader.IsDBNull(reader.GetOrdinal("Id")) ? null : reader.GetString(reader.GetOrdinal("Id")),
                                LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? null : reader.GetString(reader.GetOrdinal("LastName")),
                                //DefaultRole = reader.IsDBNull(reader.GetOrdinal("RoleName")) ? null : reader.GetString(reader.GetOrdinal("RoleName")),
                                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                EmailConfirmed = reader.IsDBNull(reader.GetOrdinal("EmailConfirmed")) ? false : reader.GetBoolean(reader.GetOrdinal("EmailConfirmed")),
                                IsFirstLogin = reader.IsDBNull(reader.GetOrdinal("IsFirstLogin")) ? false : reader.GetBoolean(reader.GetOrdinal("IsFirstLogin")),
                                Phone = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),

                            };
                        }
                    }

                }
            }

            user.Roles = new();

            ApplicationUser applicationUser = _userManager.FindByIdAsync(user.Id).GetAwaiter().GetResult();
            IList<string> roles = _userManager.GetRolesAsync(applicationUser).GetAwaiter().GetResult();

            var allRoles = _roleManager.Roles.AsNoTracking().ToList();

            foreach (ApplicationRole role in allRoles)
            {
                bool isInRole = _userManager.IsInRoleAsync(applicationUser, role.Name).GetAwaiter().GetResult();
                user.Roles.Add(new CheckBoxListItemDto { Id = role.Id, IsChecked = isInRole, Name = role.Name });
            }


            return user;
        }

        public List<UserViewModel> GetUsers(string roleName, string CreatedBy)
        {
            List<UserViewModel> users = new();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("FetchUsersByRoleName", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@RoleName", roleName ?? System.Data.SqlTypes.SqlString.Null);
                    cmd.Parameters.AddWithValue("@CreatedBy", CreatedBy ?? System.Data.SqlTypes.SqlString.Null);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new()
                            {
                                Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                                FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? null : reader.GetString(reader.GetOrdinal("FirstName")),
                                IsActive = reader.IsDBNull(reader.GetOrdinal("IsActive")) ? false : reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                Id = reader.IsDBNull(reader.GetOrdinal("Id")) ? null : reader.GetString(reader.GetOrdinal("Id")),
                                LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? null : reader.GetString(reader.GetOrdinal("LastName")),
                                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                EmailConfirmed = reader.IsDBNull(reader.GetOrdinal("EmailConfirmed")) ? false : reader.GetBoolean(reader.GetOrdinal("EmailConfirmed")),
                                IsFirstLogin = reader.IsDBNull(reader.GetOrdinal("IsFirstLogin")) ? false : reader.GetBoolean(reader.GetOrdinal("IsFirstLogin")),
                                Phone = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),

                            });
                        }
                    }

                }
            }

            foreach (var user in users)
            {
                ApplicationUser appUser = _userManager.FindByIdAsync(user.Id).GetAwaiter().GetResult();
                IList<string> roles = _userManager.GetRolesAsync(appUser).GetAwaiter().GetResult();
                user.Roles = roles.Select(role => new CheckBoxListItemDto { Name = role }).ToList();
            }

            return users;
        }


        public async Task<MessageResponse> UpdateAsync(UserViewModel model)
        {
            MessageResponse result = new();

            var context = _context;
            var scope = context.Database.BeginTransaction();
            using (scope)
            {
                try
                {
                    var userDetails = _context.ApplicationUsers.FirstOrDefault(x => x.Id == model.Id);
                    if (userDetails == null)
                    {
                        scope.Rollback();
                        result.Message = "No user record found";
                        return result;
                    }
                    if (model.Email != userDetails.Email)
                    {
                        scope.Rollback();
                        result.Message = "Email address cannot be changed.";
                        return result;
                    }

                    userDetails.Id = model.Id;
                    //userDetails.Email = model.Email;
                    userDetails.FirstName = model.FirstName;
                    userDetails.LastName = model.LastName;
                    userDetails.UserName = model.Email;
                    userDetails.IsActive = model.IsActive;
                    userDetails.EmailConfirmed = model.IsActive;
                    //userDetails.DefaultRole = model.DefaultRole;
                    userDetails.PhoneNumber = model.Phone;
                    _context.SaveChanges();

                    //var role = _context.ApplicationRoles.FirstOrDefault(r => r.Name == model.DefaultRole);
                    var userRoleQuery = _context.ApplicationUserRoles.Where(u => u.UserId == model.Id);

                    IList<string> roles = _userManager.GetRolesAsync(userDetails).GetAwaiter().GetResult();
                    //if (roles.Contains(AppRoles.SYSTEMADMIN))
                    //{
                    //    scope.Rollback();
                    //    result.Message = "You cannot update your record";
                    //    return result;
                    //}

                    //Detect if there are role changes
                    if (model.Roles.Count(x => x.IsChecked) != userRoleQuery.Count() && !model.Roles.Select(x => x.Id).SequenceEqual(userRoleQuery.Select(x => x.RoleId)))
                    {
                        List<ApplicationUserRole> userRoles = userRoleQuery.ToList();
                        _context.ApplicationUserRoles.RemoveRange(userRoles);

                        userRoles = model.Roles.Where(x => x.IsChecked).Select(x => new ApplicationUserRole
                        {
                            RoleId = x.Id,
                            UserId = model.Id,
                        }).ToList();
                        _context.ApplicationUserRoles.AddRange(userRoles);
                        _context.SaveChanges();

                    }

                    result.Message = "Record saved successfully";
                    result.Success = true;
                    scope.Commit();

                }
                catch (Exception ex)
                {
                    scope.Rollback();
                    result.Message = "Kindly try again later";
                    _logger.LogError(ex.Message, ex);
                }
                return result;
            }
        }
    }
}
