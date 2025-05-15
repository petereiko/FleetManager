using FleetManager.Business.Database.Entities;
using FleetManager.Business.DataObjects;
using FleetManager.Business.Interfaces.ComapyBranchModule;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.CompanyBranchModule
{
    public class BranchService : IBranchService
    {
        private readonly FleetManagerDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BranchService(FleetManagerDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        private long? GetCompanyId()
        {
            var email = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            return user?.CompanyId;
        }

        public async Task<IEnumerable<CompanyBranchDto>> GetBranchesForCompanyAsync()
        {
            var companyId = GetCompanyId();
            if (companyId == null) return Enumerable.Empty<CompanyBranchDto>();

            return await _context.CompanyBranches
                .AsNoTracking()
                .Where(b => b.CompanyId == companyId)
                .Include(b => b.State)
                .Include(b => b.Lga)
                .Select(b => new CompanyBranchDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Address = b.Address,
                    Email = b.Email,
                    Phone = b.Phone,
                    ManagerName = b.ManagerName,
                    ManagerPhone = b.ManagerPhone,
                    ManagerEmail = b.ManagerEmail,
                    IsHeadOffice = b.IsHeadOffice,
                    Notes = b.Notes,
                    StateId = b.StateId,
                    LgaId = b.LgaId,
                    StateName = b.State != null ? b.State.Name : "",
                    LgaName = b.Lga != null ? b.Lga.Name : ""
                }).ToListAsync();
        }

        public async Task<CompanyBranchDto?> GetBranchByIdAsync(long id)
        {
            var branch = await _context.CompanyBranches.FindAsync(id);
            if (branch == null) return null;

            return new CompanyBranchDto
            {
                Id = branch.Id,
                Name = branch.Name,
                Address = branch.Address,
                Email = branch.Email,
                Phone = branch.Phone,
                ManagerName = branch.ManagerName,
                ManagerPhone = branch.ManagerPhone,
                ManagerEmail = branch.ManagerEmail,
                IsHeadOffice = branch.IsHeadOffice,
                Notes = branch.Notes,
                StateId = branch.StateId,
                LgaId = branch.LgaId
            };
        }

        public async Task<MessageResponse> AddBranchAsync(CompanyBranchDto dto)
        {
            var companyId = GetCompanyId();
            if (companyId == null) return new() { Message = "Company not found." };

            var entity = new CompanyBranch
            {
                Name = dto.Name,
                Address = dto.Address,
                Email = dto.Email,
                Phone = dto.Phone,
                ManagerName = dto.ManagerName,
                ManagerPhone = dto.ManagerPhone,
                ManagerEmail = dto.ManagerEmail,
                IsHeadOffice = dto.IsHeadOffice,
                Notes = dto.Notes,
                StateId = dto.StateId,
                LgaId = dto.LgaId,
                CompanyId = companyId
            };

            _context.CompanyBranches.Add(entity);
            await _context.SaveChangesAsync();

            return new() { Success = true, Message = "Branch added successfully." };
        }

        public async Task<MessageResponse> UpdateBranchAsync(CompanyBranchDto dto)
        {
            var branch = await _context.CompanyBranches.FindAsync(dto.Id);
            if (branch == null) return new() { Message = "Branch not found." };

            branch.Name = dto.Name;
            branch.Address = dto.Address;
            branch.Email = dto.Email;
            branch.Phone = dto.Phone;
            branch.ManagerName = dto.ManagerName;
            branch.ManagerPhone = dto.ManagerPhone;
            branch.ManagerEmail = dto.ManagerEmail;
            branch.IsHeadOffice = dto.IsHeadOffice;
            branch.Notes = dto.Notes;
            branch.StateId = dto.StateId;
            branch.LgaId = dto.LgaId;

            _context.CompanyBranches.Update(branch);
            await _context.SaveChangesAsync();

            return new() { Success = true, Message = "Branch updated successfully." };
        }

        public async Task<bool> DeleteBranchAsync(long id)
        {
            var branch = await _context.CompanyBranches.FindAsync(id);
            if (branch == null) return false;

            _context.CompanyBranches.Remove(branch);
            await _context.SaveChangesAsync();
            return true;
        }
    }

}
