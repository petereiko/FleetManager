using FleetManager.Business.Database.Entities;
using FleetManager.Business.DataObjects.VendorDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.RentalModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.RentalModule
{
    public class RentalService : IRentalService
    {
        private readonly FleetManagerDbContext _context;
        private readonly IAuthUser _authUser;
        private readonly ILogger<RentalService> _logger;

        public RentalService(
            FleetManagerDbContext context,
            IAuthUser authUser,
            ILogger<RentalService> logger)
        {
            _context = context;
            _authUser = authUser;
            _logger = logger;
        }

        public async Task<MessageResponse> ApplyForRentalAsync(VehicleRentalOnboardingDto dto)
        {
            var resp = new MessageResponse();
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Persist rental request
                var rental = new VehicleToCompanyRental
                {
                    VendorId = dto.VendorId,
                    CompanyBranhcId = dto.CompanyBranchId,
                    RentalStatus = RentalStatus.Reserved,
                    VehicleCount = dto.VehicleCount,
                    Comment = dto.Comment,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    CreatedBy = _authUser.UserId,
                    CreatedDate = DateTime.UtcNow
                };
                _context.Add(rental);
                await _context.SaveChangesAsync();

                // Save request document
                if (dto.RequestFile != null)
                {
                    var doc = await SaveRentalFileAsync(rental.Id, dto.RequestFile, "RentalRequests");
                    rental.RentalRequestFileName = doc.FileName;
                    rental.RentalRequestFilePath = doc.FilePath;
                    await _context.SaveChangesAsync();
                }

                await tx.CommitAsync();
                resp.Success = true;
                resp.Message = "Rental request submitted.";
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ApplyForRentalAsync failed");
                await tx.RollbackAsync();
                resp.Message = "Failed to submit rental request.";
                return resp;
            }
        }

        public async Task<MessageResponse<VehicleRentalDto>> UpdateRentalAsync(VehicleRentalDto dto)
        {
            var resp = new MessageResponse<VehicleRentalDto>();
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var entity = await _context.VehicleToCompanyRentals
                    .FirstOrDefaultAsync(r => r.Id == dto.Id);
                if (entity == null)
                {
                    resp.Message = "Rental not found.";
                    return resp;
                }

                // map fields
                entity.RentalStatus = dto.RentalStatus;
                entity.VehicleCount = dto.VehicleCount;
                entity.Comment = dto.Comment;
                entity.ActualReturnedDate = dto.ActualReturnedDate;
                entity.EndDate = dto.EndDate;
                entity.ModifiedBy = _authUser.UserId;
                entity.ModifiedDate = DateTime.UtcNow;

                // handle agreement document
                if (dto.AgreementFile != null)
                {
                    var doc = await SaveRentalFileAsync(entity.Id, dto.AgreementFile, "RentalAgreements");
                    entity.RentalAgreementFileName = doc.FileName;
                    entity.RentalAgreementFilePath = doc.FilePath;
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                resp.Success = true;
                resp.Result = dto;
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateRentalAsync failed");
                await tx.RollbackAsync();
                resp.Message = "Failed to update rental.";
                return resp;
            }
        }

        public async Task<VehicleRentalApplyViewModel> GetRentalApplyViewModelAsync(
    long vendorId,
    long companyBranchId)
        {
            // fetch both in one place
            var vendor = await _context.Vendors
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == vendorId);

            var branch = await _context.CompanyBranches
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == companyBranchId);

            if (vendor == null || branch == null)
                throw new InvalidOperationException("Vendor or Company Branch not found.");

            return new VehicleRentalApplyViewModel
            {
                VendorId = vendor.Id,
                VendorName = vendor.VendorName,
                CompanyBranchId = branch.Id,
                CompanyBranchName = branch.Name,

                // initialize the DTO for binding
                Rental = new VehicleRentalOnboardingDto
                {
                    VendorId = vendor.Id,
                    CompanyBranchId = branch.Id
                }
            };
        }


        public async Task<VehicleRentalAgreementDto?> GetRentalForAgreementAsync(long id)
        {
            var rental = await _context.VehicleToCompanyRentals.FindAsync(id);
            return rental == null ? null : new VehicleRentalAgreementDto { Id = rental.Id };
        }

        public async Task<MessageResponse> DeleteRentalAsync(long id)
        {
            var resp = new MessageResponse();
            try
            {
                var entity = await _context.VehicleToCompanyRentals.FindAsync(id);
                if (entity == null)
                {
                    resp.Message = "Rental not found.";
                    return resp;
                }
                _context.VehicleToCompanyRentals.Remove(entity);
                await _context.SaveChangesAsync();
                resp.Success = true;
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteRentalAsync failed");
                resp.Message = "Failed to delete rental.";
                return resp;
            }
        }

        public async Task<VehicleRentalDto?> GetRentalByIdAsync(long id)
        {
            var v = await _context.VehicleToCompanyRentals
                .AsNoTracking()
                .Include(r => r.Vendor)
                .Include(r => r.CompanyBranch)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (v == null) return null;

            return new VehicleRentalDto
            {
                Id = v.Id,
                VendorId = v.VendorId,
                VendorName = v.Vendor.VendorName,
                CompanyBranchId = v.CompanyBranhcId,
                BranchName = v.CompanyBranch.Name,
                RentalStatus = v.RentalStatus,
                VehicleCount = v.VehicleCount,
                Comment = v.Comment,
                StartDate = v.StartDate,
                EndDate = v.EndDate,
                ActualReturnedDate = v.ActualReturnedDate,
                RequestFilePath = v.RentalRequestFilePath,
                AgreementFilePath = v.RentalAgreementFilePath
            };
        }

        public async Task<List<VehicleRentalDto>> GetRentalsForBranchAsync(long? branchId = null)
        {
            var qb = _context.VehicleToCompanyRentals
                .AsNoTracking()
                .Include(r => r.Vendor)
                .Include(r => r.CompanyBranch)
                .Where(r => r.CompanyBranhcId == (_authUser.CompanyBranchId ?? 0));

            if (branchId.HasValue)
                qb = qb.Where(r => r.CompanyBranhcId == branchId.Value);

            var list = await qb.ToListAsync();
            return list.Select(v => new VehicleRentalDto
            {
                Id = v.Id,
                VendorId = v.VendorId,
                VendorName = v.Vendor.VendorName,
                CompanyBranchId = v.CompanyBranhcId,
                BranchName = v.CompanyBranch.Name,
                RentalStatus = v.RentalStatus,
                VehicleCount = v.VehicleCount,
                StartDate = v.StartDate,
                EndDate = v.EndDate
            }).ToList();
        }

        public List<SelectListItem> GetRentalStatusOptions()
            => Enum.GetValues<RentalStatus>()
                .Select(rs => new SelectListItem
                {
                    Value = ((int)rs).ToString(),
                    Text = rs.ToString()
                })
                .ToList();

        

        private async Task<(string FileName, string FilePath)> SaveRentalFileAsync(long rentalId, IFormFile file, string subfolder)
        {
            var uploadRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "vendorRentals", subfolder);
            Directory.CreateDirectory(uploadRoot);
            var unique = $"{Guid.NewGuid()}_{file.FileName}";
            var full = Path.Combine(uploadRoot, unique);
            using var stream = new FileStream(full, FileMode.Create);
            await file.CopyToAsync(stream);
            var relative = $"/vendorRentals/{subfolder}/{unique}";
            return (file.FileName, relative);
        }
    }

}
