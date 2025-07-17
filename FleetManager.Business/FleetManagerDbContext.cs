using System.Net.Sockets;
using System.Reflection.Emit;
using FleetManager.Business.Database.Entities;
using FleetManager.Business.Database.Entities.MaintenanceTicket;
using FleetManager.Business.Database.IdentityModels;
using FleetManager.Business.DataObjects.ReportsDto;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.Business
{
    public class FleetManagerDbContext: IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public FleetManagerDbContext(DbContextOptions<FleetManagerDbContext> options) : base(options)
        {

        }

        // (Optional) add this for discoverability:
        public DbSet<FuelLogWithPrev> FuelLogWithPrev { get; set; }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            // map your view
            mb.Entity<FuelLogWithPrev>(eb =>
            {
                eb.ToView("vw_FuelLogWithPrev");
                eb.HasNoKey();

                // ONLY if you added navigation props on FuelLogWithPrev:
                // eb.HasOne(fl => fl.Vehicle)
                //   .WithMany()
                //   .HasForeignKey(fl => fl.VehicleId);
                // eb.HasOne(fl => fl.Driver)
                //   .WithMany()
                //   .HasForeignKey(fl => fl.DriverId);
            });

            // One-to-one: MaintenanceTicket ↔ Invoice
            mb.Entity<MaintenanceTicket>()
                .HasOne(t => t.Invoice)
                .WithOne(i => i.MaintenanceTicket)
                .HasForeignKey<Invoice>(i => i.MaintenanceTicketId);
        }



        #region Identities
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<ApplicationRole> ApplicationRoles { get; set; }
        public DbSet<ApplicationUserRole> ApplicationUserRoles { get; set; }
        #endregion

        #region Domains
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<CompanyBranch> CompanyBranches { get; set; }
        public DbSet<CompanyAdmin> CompanyAdmins { get; set; }
        public DbSet<ContactDirectory> ContactDirectories { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<DriverDocument> DriverDocuments { get; set; }
        public DbSet<DriverVehicle> DriverVehicles { get; set; }
        public DbSet<DriverVehicleLocation> DriverVehicleLocations { get; set; }
        public DbSet<DriverDutyOfCare> DriverDutyOfCares { get; set; }
        public DbSet<DriverViolation> DriverViolations { get; set; }


        public DbSet<EmailLog> EmailLogs { get; set; }
        public DbSet<FineAndToll> FineAndTolls { get; set; }
        public DbSet<FuelLog> FuelLogs { get; set; }

        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<MaintenanceTicket> MaintenanceTickets { get; set; }
        public DbSet<MaintenanceTicketItem> MaintenanceTicketItems { get; set; }
        public DbSet<LGA> LGAs {  get; set; }
        public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }

        public DbSet<Notification> Notifications { get; set; }
        
        public DbSet<NextOfKin> NextOfKins { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<VehicleModel> VehicleModels { get; set; }
        public DbSet<VehicleMake> VehicleMakes { get; set; }
        public DbSet<VehicleDocument> VehicleDocuments { get; set; }
        public DbSet<VehiclePartCategory> VehiclePartCategories { get; set; }
        public DbSet<VehiclePart> VehicleParts { get; set; }

        public DbSet<VehicleToCompanyRental> VehicleToCompanyRentals { get; set; }
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<VendorCategory> VendorCategories { get; set; }

        #endregion
    }
}
