using System.Net.Sockets;
using FleetManager.Business.Database.Entities;
using FleetManager.Business.Database.IdentityModels;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.Business
{
    public class FleetManagerDbContext: IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        #region Identities
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<ApplicationRole> ApplicationRoles { get; set; }
        public DbSet<ApplicationUserRole> ApplicationUserRoles { get; set; }
        #endregion

        #region Domains
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<VehicleDocument> VehicleDocuments { get; set; }
        public DbSet<DriverVehicle> DriverVehicles { get; set; }
        public DbSet<DriverVehicleLocation> DriverVehicleLocations { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<NextOfKin> NextOfKins { get; set; }

        #endregion
    }
}
