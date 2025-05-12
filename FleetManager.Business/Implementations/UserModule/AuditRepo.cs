
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Data;
using Microsoft.Extensions.Configuration;
using FleetManager.Business;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.Database.Entities;
using FleetManager.Business.DataObjects;

namespace DVLA.Business.UserModule
{
    public class AuditRepo : IAuditRepo
    {
        private readonly FleetManagerDbContext _context;
        private readonly ILogger<AuditRepo> _logger;
        private readonly string _connectionString;
        private readonly IUserService _userService;
        private readonly IAuthUser _authUser;

        private static object initLock = new object();

        public AuditRepo(FleetManagerDbContext context, ILogger<AuditRepo> logger, IConfiguration configuration, IUserService userService, IAuthUser authUser)
        {
            _context = context;
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _userService = userService;
            _authUser = authUser;
        }

        public void AddAudit(long moduleActionId, string description)
        {
            var user = _context.ApplicationUsers.FirstOrDefault(x => x.Id == _authUser.UserId);

            var auditLog = new ActivityLog
            {
                CreatedBy = user?.LastName == null ? user?.LastName : user?.FirstName,
                Id = moduleActionId,
                Description = description,
                CreatedDate = DateTime.Now
            };
            _context.ActivityLogs.Add(auditLog);
            _context.SaveChanges();
        }


        public async Task<List<ActivityModel>> GetAuditAsync(AuditFilterModel model)
        {
            var result = new List<ActivityModel>();
            return result;
        }



    }
}
