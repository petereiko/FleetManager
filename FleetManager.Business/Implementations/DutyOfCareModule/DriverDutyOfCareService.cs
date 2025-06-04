using FleetManager.Business.Database.Entities;
using FleetManager.Business.DataObjects;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.UtilityModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FleetManager.Business.Interfaces.DutyOfCareModule;

namespace FleetManager.Business.Implementations.DutyOfCareModule
{
    public class DriverDutyOfCareService : IDriverDutyOfCareService
    {
        private readonly FleetManagerDbContext _context;
        private readonly IAuthUser _authUser;
        private readonly ILogger<DriverDutyOfCareService> _logger;

        public DriverDutyOfCareService(
            FleetManagerDbContext context,
            IAuthUser authUser,
            ILogger<DriverDutyOfCareService> logger)
        {
            _context = context;
            _authUser = authUser;
            _logger = logger;
        }

        /// <summary>
        /// Ensures the current user is a Company Admin, Company Owner, or Super Admin.
        /// </summary>
        private void EnsureAdminOrOwner()
        {
            var roles = (_authUser.Roles ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim());

            if (!roles.Contains("Company Admin")
             && !roles.Contains("Company Owner")
             && !roles.Contains("Super Admin"))
            {
                throw new UnauthorizedAccessException("You do not have permission to manage duty‑of‑care records.");
            }
        }

        public IQueryable<DriverDutyOfCareDto> QueryByDriver(long driverId)
        {
            EnsureAdminOrOwner();

            return _context.DriverDutyOfCares
                .AsNoTracking()
                .Where(d => d.DriverId == driverId)
                .Select(entity => Map(entity));
        }

        public IQueryable<DriverDutyOfCareDto> QueryByVehicle(long vehicleId)
        {
            EnsureAdminOrOwner();

            return _context.DriverDutyOfCares
                .AsNoTracking()
                .Where(d => d.VehicleId == vehicleId)
                .Select(entity => Map(entity));
        }

        public async Task<DriverDutyOfCareDto?> GetByIdAsync(long id)
        {
            EnsureAdminOrOwner();

            var entity = await _context.DriverDutyOfCares
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id)
                .ConfigureAwait(false);

            return entity == null ? null : Map(entity);
        }

        public async Task<MessageResponse<DriverDutyOfCareDto>> CreateAsync(
            DriverDutyOfCareDto dto,
            string createdBy)
        {
            EnsureAdminOrOwner();
            var resp = new MessageResponse<DriverDutyOfCareDto>();

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var entity = new DriverDutyOfCare
                {
                    DriverId = dto.DriverId,
                    VehicleId = dto.VehicleId,
                    Date = dto.Date == default ? DateTime.UtcNow : dto.Date,

                    VehiclePreCheckCompleted = dto.VehiclePreCheckCompleted,
                    VehicleConditionNotes = dto.VehicleConditionNotes ?? string.Empty,

                    IsFitToDrive = dto.IsFitToDrive,
                    HealthDeclarationNotes = dto.HealthDeclarationNotes ?? string.Empty,

                    HasValidLicense = dto.HasValidLicense,
                    IsAwareOfCompanyPolicies = dto.IsAwareOfCompanyPolicies,
                    HasReviewedDrivingHours = dto.HasReviewedDrivingHours,

                    LastRestPeriod = dto.LastRestPeriod,
                    ReportsFatigue = dto.ReportsFatigue,

                    ReportsVehicleIssues = dto.ReportsVehicleIssues,
                    ReportedIssuesDetails = dto.ReportedIssuesDetails ?? string.Empty,

                    ConfirmsAccuracyOfInfo = dto.ConfirmsAccuracyOfInfo,
                    DeclarationTimestamp = dto.DeclarationTimestamp == default
                                                  ? DateTime.UtcNow
                                                  : dto.DeclarationTimestamp,

                    DutyOfCareRecordType = dto.DutyOfCareRecordType,
                    DutyOfCareStatus = dto.DutyOfCareStatus,

                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                _context.DriverDutyOfCares.Add(entity);
                await _context.SaveChangesAsync().ConfigureAwait(false);
                await tx.CommitAsync().ConfigureAwait(false);

                dto.Id = entity.Id;
                dto.CreatedAt = entity.CreatedAt;
                dto.CreatedBy = entity.CreatedBy;
                dto.ModifiedDate = null;
                dto.ModifiedBy = null;

                resp.Success = true;
                resp.Result = dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating DriverDutyOfCare record");
                await tx.RollbackAsync().ConfigureAwait(false);
                resp.Message = "An unexpected error occurred while creating the record.";
            }

            return resp;
        }

        public async Task<MessageResponse<DriverDutyOfCareDto>> UpdateAsync(
            DriverDutyOfCareDto dto,
            string modifiedBy)
        {
            EnsureAdminOrOwner();
            var resp = new MessageResponse<DriverDutyOfCareDto>();

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var entity = await _context.DriverDutyOfCares
                    .FirstOrDefaultAsync(d => d.Id == dto.Id).ConfigureAwait(false);

                if (entity == null)
                {
                    resp.Message = "Record not found.";
                    return resp;
                }

                // Map incoming changes
                entity.Date = dto.Date;
                entity.VehiclePreCheckCompleted = dto.VehiclePreCheckCompleted;
                entity.VehicleConditionNotes = dto.VehicleConditionNotes ?? string.Empty;

                entity.IsFitToDrive = dto.IsFitToDrive;
                entity.HealthDeclarationNotes = dto.HealthDeclarationNotes ?? string.Empty;

                entity.HasValidLicense = dto.HasValidLicense;
                entity.IsAwareOfCompanyPolicies = dto.IsAwareOfCompanyPolicies;
                entity.HasReviewedDrivingHours = dto.HasReviewedDrivingHours;

                entity.LastRestPeriod = dto.LastRestPeriod;
                entity.ReportsFatigue = dto.ReportsFatigue;

                entity.ReportsVehicleIssues = dto.ReportsVehicleIssues;
                entity.ReportedIssuesDetails = dto.ReportedIssuesDetails ?? string.Empty;

                entity.ConfirmsAccuracyOfInfo = dto.ConfirmsAccuracyOfInfo;
                entity.DeclarationTimestamp = dto.DeclarationTimestamp;

                entity.DutyOfCareRecordType = dto.DutyOfCareRecordType;
                entity.DutyOfCareStatus = dto.DutyOfCareStatus;

                entity.ModifiedBy = modifiedBy;
                entity.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync().ConfigureAwait(false);
                await tx.CommitAsync().ConfigureAwait(false);

                resp.Success = true;
                resp.Result = dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating DriverDutyOfCare record Id {Id}", dto.Id);
                await tx.RollbackAsync().ConfigureAwait(false);
                resp.Message = "An unexpected error occurred while updating the record.";
            }

            return resp;
        }

        public async Task<MessageResponse> DeleteAsync(long id)
        {
            EnsureAdminOrOwner();
            var resp = new MessageResponse();

            try
            {
                var entity = await _context.DriverDutyOfCares.FindAsync(id).ConfigureAwait(false);
                if (entity == null)
                {
                    resp.Message = "Record not found.";
                    return resp;
                }

                _context.DriverDutyOfCares.Remove(entity);
                await _context.SaveChangesAsync().ConfigureAwait(false);

                resp.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting DriverDutyOfCare record Id {Id}", id);
                resp.Message = "An unexpected error occurred while deleting the record.";
            }

            return resp;
        }

        //public async Task<DriverDutyOfCareDto> GetDutyOfCareByIdAsync(int id)
        //{
        //    var duty = await _context.DriverDutiesOfCare
        //        .Include(d => d.Driver)
        //        .Include(d => d.AssignedBy)
        //        .FirstOrDefaultAsync(d => d.Id == id);

        //    if (duty == null) return null;

        //    return new DriverDutyOfCareDto
        //    {
        //        Id = duty.Id,
        //        DriverId = duty.DriverId,
        //        DriverFullName = duty.Driver.FirstName + " " + duty.Driver.LastName,
        //        AssignedById = duty.AssignedById,
        //        AssignedByFullName = duty.AssignedBy?.FullName,
        //        DutyDescription = duty.DutyDescription,
        //        AssignedDate = duty.AssignedDate,
        //        ExpiryDate = duty.ExpiryDate
        //    };
        //}


        public IQueryable<DriverDutyOfCareDto> QueryAll()
        {
            EnsureAdminOrOwner();

            return _context.DriverDutyOfCares
                .AsNoTracking()
                .Select(d => new DriverDutyOfCareDto
                {
                    Id = d.Id,
                    DriverId = d.DriverId ?? 0,
                    VehicleId = d.VehicleId ?? 0,
                    Date = d.Date,
                    VehiclePreCheckCompleted = d.VehiclePreCheckCompleted,
                    VehicleConditionNotes = d.VehicleConditionNotes,
                    IsFitToDrive = d.IsFitToDrive,
                    HealthDeclarationNotes = d.HealthDeclarationNotes,
                    HasValidLicense = d.HasValidLicense,
                    IsAwareOfCompanyPolicies = d.IsAwareOfCompanyPolicies,
                    HasReviewedDrivingHours = d.HasReviewedDrivingHours,
                    LastRestPeriod = d.LastRestPeriod,
                    ReportsFatigue = d.ReportsFatigue,
                    ReportsVehicleIssues = d.ReportsVehicleIssues,
                    ReportedIssuesDetails = d.ReportedIssuesDetails,
                    ConfirmsAccuracyOfInfo = d.ConfirmsAccuracyOfInfo,
                    DeclarationTimestamp = d.DeclarationTimestamp,
                    CreatedAt = d.CreatedAt,
                    CreatedBy = d.CreatedBy,
                    DutyOfCareRecordType = d.DutyOfCareRecordType,
                    DutyOfCareStatus = d.DutyOfCareStatus,

                    // You probably want to include driver name and vehicle name as well:
                    //DriverName = _context.Drivers
                    //               .Where(x => x.Id == d.DriverId)
                    //               .Select(x => x.Id.ToString()) // placeholder: join to ApplicationUser or use navigation property
                    //               .FirstOrDefault(),

                    //VehicleDescription = _context.Vehicles
                    //                .Where(v => v.Id == d.VehicleId)
                    //                .Select(v => v.Make + " " + v.Model)
                    //                .FirstOrDefault()
                });
        }

        /// <summary>
        /// Maps the EF entity to our DTO.
        /// </summary>
        private static DriverDutyOfCareDto Map(DriverDutyOfCare d) => new DriverDutyOfCareDto
        {
            Id = d.Id,
            DriverId = d.DriverId!.Value,
            VehicleId = d.VehicleId!.Value,
            Date = d.Date,

            VehiclePreCheckCompleted = d.VehiclePreCheckCompleted,
            VehicleConditionNotes = d.VehicleConditionNotes,

            IsFitToDrive = d.IsFitToDrive,
            HealthDeclarationNotes = d.HealthDeclarationNotes,

            HasValidLicense = d.HasValidLicense,
            IsAwareOfCompanyPolicies = d.IsAwareOfCompanyPolicies,
            HasReviewedDrivingHours = d.HasReviewedDrivingHours,

            LastRestPeriod = d.LastRestPeriod,
            ReportsFatigue = d.ReportsFatigue,

            ReportsVehicleIssues = d.ReportsVehicleIssues,
            ReportedIssuesDetails = d.ReportedIssuesDetails,

            ConfirmsAccuracyOfInfo = d.ConfirmsAccuracyOfInfo,
            DeclarationTimestamp = d.DeclarationTimestamp,

            CreatedAt = d.CreatedAt,
            CreatedBy = d.CreatedBy,
            ModifiedDate = d.ModifiedDate,
            ModifiedBy = d.ModifiedBy,

            DutyOfCareRecordType = d.DutyOfCareRecordType,
            DutyOfCareStatus = d.DutyOfCareStatus
        };
    }
}
