
using FleetManager.Business.Database.Entities;
using FleetManager.Business.DataObjects.VehicleDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.Interfaces.VehicleModule;
using FleetManager.Business.UtilityModels;
using FleetManager.Business;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Net;
using FleetManager.Business.DataObjects;


namespace FleetManager.Business.Implementations.VehicleModule
{
    public class AdminVehicleService : IAdminVehicleService
    {
        private readonly FleetManagerDbContext _context;
        private readonly IAuthUser _authUser;
        private readonly ILogger<AdminVehicleService> _logger;
        private readonly IHttpClientFactory _clientFactory;

        public AdminVehicleService(
            FleetManagerDbContext context,
            IAuthUser authUser,
            ILogger<AdminVehicleService> logger,
            IHttpClientFactory clientFactory)
        {
            _context = context;
            _authUser = authUser;
            _logger = logger;
            _clientFactory = clientFactory;
        }

        private void EnsureAdminOrOwner()
        {
            var roles = (_authUser.Roles ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim());

            if (!roles.Contains("Company Admin")
             && !roles.Contains("Company Owner")
             && !roles.Contains("Super Admin"))
            {
                throw new UnauthorizedAccessException("You do not have permission to manage vehicles.");
            }
        }

        public async Task<MessageResponse<VehicleDto>> CreateVehicleAsync(VehicleDto dto, string createdByUserId)
        {
            EnsureAdminOrOwner();
            var resp = new MessageResponse<VehicleDto>();

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var entity = new Vehicle
                {
                    VehicleMakeId = dto.VehicleMakeId,
                    VehicleModelId = dto.VehicleModelId,
                    Year = dto.Year,
                    VIN = dto.VIN,
                    PlateNo = dto.PlateNo,
                    Color = dto.Color,
                    EngineNumber = dto.EngineNumber,
                    ChassisNumber = dto.ChassisNumber,
                    RegistrationDate = dto.RegistrationDate,
                    LastServiceDate = dto.LastServiceDate,
                    Mileage = dto.Mileage,
                    FuelType = dto.FuelType,
                    TransmissionType = dto.TransmissionType,
                    InsuranceCompany = dto.InsuranceCompany,
                    InsuranceExpiryDate = dto.InsuranceExpiryDate,
                    RoadWorthyExpiryDate = dto.RoadWorthyExpiryDate,
                    CompanyBranchId = dto.CompanyBranchId,
                    VehicleStatus = dto.VehicleStatus,
                    VehicleType = dto.VehicleType,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = createdByUserId,
                    IsActive = true
                };
               
                _context.Vehicles.Add(entity);
                await _context.SaveChangesAsync();

                // Save uploaded files
                var saved = new List<VehicleDocument>();
                saved.AddRange(await SaveVehicleFilesAsync(entity.Id, dto.DocumentFiles, VehicleDocumentType.Document));
                saved.AddRange(await SaveVehicleFilesAsync(entity.Id, dto.PhotoFiles, VehicleDocumentType.Photo));
                if (saved.Any())
                {
                    _context.VehicleDocuments.AddRange(saved);
                    await _context.SaveChangesAsync();
                }

                await tx.CommitAsync();

                dto.Id = entity.Id;
                resp.Success = true;
                resp.Result = dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create vehicle");
                await tx.RollbackAsync();
                resp.Message = "An error occurred creating the vehicle.";
            }

            return resp;
        }

        public async Task<MessageResponse<VehicleDto>> UpdateVehicleAsync(VehicleDto dto, string modifiedByUserId)
        {
            EnsureAdminOrOwner();
            var resp = new MessageResponse<VehicleDto>();

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var vehicle = await _context.Vehicles.FindAsync(dto.Id);
                if (vehicle == null)
                {
                    resp.Message = "Vehicle not found.";
                    return resp;
                }

                // map changes
                vehicle.VehicleMakeId = dto.VehicleMakeId;
                vehicle.VehicleModelId = dto.VehicleModelId;
                vehicle.Year = dto.Year;
                vehicle.VIN = dto.VIN;
                vehicle.PlateNo = dto.PlateNo;
                vehicle.Color = dto.Color;
                vehicle.EngineNumber = dto.EngineNumber;
                vehicle.ChassisNumber = dto.ChassisNumber;
                vehicle.RegistrationDate = dto.RegistrationDate;
                vehicle.LastServiceDate = dto.LastServiceDate;
                vehicle.Mileage = dto.Mileage;
                vehicle.FuelType = dto.FuelType;
                vehicle.TransmissionType = dto.TransmissionType;
                vehicle.InsuranceCompany = dto.InsuranceCompany;
                vehicle.InsuranceExpiryDate = dto.InsuranceExpiryDate;
                vehicle.RoadWorthyExpiryDate = dto.RoadWorthyExpiryDate;
                vehicle.CompanyBranchId = dto.CompanyBranchId;
                vehicle.VehicleStatus = dto.VehicleStatus;
                vehicle.VehicleType = dto.VehicleType;
                vehicle.ModifiedDate = DateTime.UtcNow;
                vehicle.ModifiedBy = modifiedByUserId;

                await _context.SaveChangesAsync();

                // Save any new uploads
                var newDocs = new List<VehicleDocument>();
                newDocs.AddRange(await SaveVehicleFilesAsync(vehicle.Id, dto.DocumentFiles, VehicleDocumentType.Document));
                newDocs.AddRange(await SaveVehicleFilesAsync(vehicle.Id, dto.PhotoFiles, VehicleDocumentType.Photo));
                if (newDocs.Any())
                {
                    _context.VehicleDocuments.AddRange(newDocs);
                    await _context.SaveChangesAsync();
                }

                await tx.CommitAsync();

                resp.Success = true;
                resp.Result = dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update vehicle");
                await tx.RollbackAsync();
                resp.Message = "An error occurred updating the vehicle.";
            }

            return resp;
        }

        public async Task<MessageResponse> DeleteVehicleAsync(long id)
        {
            EnsureAdminOrOwner();
            var resp = new MessageResponse();

            try
            {
                var entity = await _context.Vehicles.FindAsync(id);
                if (entity == null)
                {
                    resp.Message = "Vehicle not found.";
                    return resp;
                }
                _context.Vehicles.Remove(entity);
                await _context.SaveChangesAsync();

                resp.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete vehicle");
                resp.Message = "An error occurred deleting the vehicle.";
            }

            return resp;
        }

        public async Task<MessageResponse> DeleteVehicleDocumentAsync(long documentId)
        {
            EnsureAdminOrOwner();
            var resp = new MessageResponse();

            var doc = await _context.VehicleDocuments.FindAsync(documentId);
            if (doc == null)
            {
                resp.Message = "Document not found.";
                return resp;
            }
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", doc.FilePath.TrimStart('/'));
            if (File.Exists(fullPath))
                File.Delete(fullPath);

            _context.VehicleDocuments.Remove(doc);
            await _context.SaveChangesAsync();

            resp.Success = true;
            return resp;
        }

        public async Task<VehicleDto> GetVehicleByIdAsync(long id)
        {
            EnsureAdminOrOwner();
            var v = await _context.Vehicles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (v == null) return null;

            var docs = await _context.VehicleDocuments
                .AsNoTracking()
                .Where(d => d.VehicleId == v.Id)
                .ToListAsync();

            return new VehicleDto
            {
                Id = v.Id,
                Make = v.VehicleMake.Name,
                Model = v.VehicleModel.Name,
                Year = v.Year,
                VIN = v.VIN,
                PlateNo = v.PlateNo,
                Color = v.Color,
                EngineNumber = v.EngineNumber,
                ChassisNumber = v.ChassisNumber,
                RegistrationDate = v.RegistrationDate,
                LastServiceDate = v.LastServiceDate,
                Mileage = v.Mileage,
                FuelType = v.FuelType,
                TransmissionType = v.TransmissionType,
                InsuranceCompany = v.InsuranceCompany,
                InsuranceExpiryDate = v.InsuranceExpiryDate,
                RoadWorthyExpiryDate = v.RoadWorthyExpiryDate,
                CompanyBranchId = v.CompanyBranchId ?? 0,
                VehicleStatus = v.VehicleStatus,
                VehicleType = v.VehicleType,

                Documents = docs
                    .Where(d => d.DocumentType == VehicleDocumentType.Document)
                    .Select(d => new VehicleDocumentDto
                    {
                        Id = d.Id,
                        FileName = d.FileName,
                        FilePath = d.FilePath,
                        DocumentType = d.DocumentType
                    })
                    .ToList(),

                Photos = docs
                    .Where(d => d.DocumentType == VehicleDocumentType.Photo)
                    .Select(d => new VehicleDocumentDto
                    {
                        Id = d.Id,
                        FileName = d.FileName,
                        FilePath = d.FilePath,
                        DocumentType = d.DocumentType
                    })
                    .ToList()
            };
        }

        public async Task<List<VehicleListItemDto>> GetVehiclesAsync(VehicleFilterDto filter)
        {
            EnsureAdminOrOwner();
            var query = _context.Vehicles.AsNoTracking()
                .Where(v => v.CompanyBranch.CompanyId == _authUser.CompanyId.Value);

            if (filter.BranchId.HasValue)
                query = query.Where(v => v.CompanyBranchId == filter.BranchId.Value);
            if (filter.Status.HasValue)
                query = query.Where(v => v.VehicleStatus == filter.Status.Value);
            if (filter.Type.HasValue)
                query = query.Where(v => v.VehicleType == filter.Type.Value);
            if (!string.IsNullOrWhiteSpace(filter.Search))
                query = query.Where(v => v.VehicleMake.Name.Contains(filter.Search)
                                      || v.VehicleModel.Name.Contains(filter.Search)
                                      || v.PlateNo.Contains(filter.Search));

            return await query
                .Select(v => new VehicleListItemDto
                {
                    Id = v.Id,
                    Make = v.VehicleMake.Name,
                    Model = v.VehicleModel.Name,
                    Year = v.Year,
                    PlateNo = v.PlateNo,
                    Status = v.VehicleStatus.ToString(),
                    BranchName = v.CompanyBranch.Name,
                    MainImagePath = v.VehicleDocuments
                        .Where(d => d.DocumentType == VehicleDocumentType.Photo)
                        .Select(d => d.FilePath)
                        .FirstOrDefault()
                })
                .ToListAsync();
        }

        public IQueryable<VehicleListItemDto> QueryVehicles(VehicleFilterDto filter)
        {
            EnsureAdminOrOwner();

            var q = _context.Vehicles
                .AsNoTracking()
                .Where(v => v.CompanyBranch.CompanyId == _authUser.CompanyId.Value);

            if (filter.BranchId.HasValue)
                q = q.Where(v => v.CompanyBranchId == filter.BranchId.Value);
            if (filter.Status.HasValue)
                q = q.Where(v => v.VehicleStatus == filter.Status.Value);
            if (filter.Type.HasValue)
                q = q.Where(v => v.VehicleType == filter.Type.Value);
            if (!string.IsNullOrWhiteSpace(filter.Search))
                q = q.Where(v =>
                    v.VehicleMake.Name.Contains(filter.Search) ||
                    v.VehicleModel.Name.Contains(filter.Search) ||
                    v.PlateNo.Contains(filter.Search));

            return q
                .Select(v => new VehicleListItemDto
                {
                    Id = v.Id,
                    Make = v.VehicleMake.Name,
                    Color = v.Color,
                    TransmissionType = v.TransmissionType,
                    LastServiceDate = v.LastServiceDate,
                    Model = v.VehicleModel.Name,
                    Year = v.Year,
                    PlateNo = v.PlateNo,
                    Status = v.VehicleStatus.ToString(),
                    BranchName = v.CompanyBranch.Name,
                    MainImagePath = v.VehicleDocuments
                        .Where(d => d.DocumentType == VehicleDocumentType.Photo)
                        .Select(d => d.FilePath)
                        .FirstOrDefault()
                });
        }


        private async Task<List<VehicleDocument>> SaveVehicleFilesAsync(
            long vehicleId, List<IFormFile> files, VehicleDocumentType docType)
        {
            var savedDocs = new List<VehicleDocument>();
            var uploadRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "VehicleImages");

            if (!Directory.Exists(uploadRoot))
                Directory.CreateDirectory(uploadRoot);

            foreach (var file in files ?? Enumerable.Empty<IFormFile>())
            {
                if (file.Length > 0)
                {
                    var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                    var fullPath = Path.Combine(uploadRoot, uniqueFileName);

                    using var stream = new FileStream(fullPath, FileMode.Create);
                    await file.CopyToAsync(stream);

                    savedDocs.Add(new VehicleDocument
                    {
                        VehicleId = vehicleId,
                        FileName = file.FileName,
                        FilePath = $"/VehicleImages/{uniqueFileName}",
                        DocumentType = docType
                    });
                }
            }

            return savedDocs;
        }

        public List<SelectListItem> GetFuelTypeOptions()
        {
            return Enum.GetValues<FuelType>()
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e.ToString()
                })
                .ToList();
        }

        // Repeat for the other enums...
        public List<SelectListItem> GetTransmissionTypeOptions() =>
            Enum.GetValues<TransmissionType>()
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e.ToString()
                })
                .ToList();

        public List<SelectListItem> GetStatusOptions() =>
            Enum.GetValues<VehicleStatus>()
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e.ToString()
                })
                .ToList();

        public List<SelectListItem> GetVehicleTypeOptions() =>
            Enum.GetValues<VehicleType>()
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e.ToString()
                })
                .ToList();

        public async Task<List<SelectListItem>> GetBranchOptionsAsync(long companyId)
        {
            return await _context.CompanyBranches
                .AsNoTracking()
                .Where(b => b.CompanyId == companyId)
                .Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = b.Name
                })
                .ToListAsync();
        }
        public async Task<MessageResponse> UpdateVehicleStatusAsync(long vehicleId, VehicleStatus newStatus, string modifiedBy)
        {
            EnsureAdminOrOwner();
            var resp = new MessageResponse();
            try
            {
                var v = await _context.Vehicles.FindAsync(vehicleId);
                if (v == null)
                {
                    resp.Message = "Vehicle not found.";
                    return resp;
                }
                v.VehicleStatus = newStatus;
                v.ModifiedDate = DateTime.UtcNow;
                v.ModifiedBy = modifiedBy;
                await _context.SaveChangesAsync();
                resp.Success = true;
                resp.Message = "Status updated.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status");
                resp.Message = "Could not update status.";
            }
            return resp;
        }


        public List<SelectListItem> GetVehicleMakes()
        {
            return _context.VehicleMakes
                .AsNoTracking()
                .Where(vm => vm.IsActive)
                .OrderBy(vm => vm.Name)
                .Select(vm => new SelectListItem
                {
                    Value = vm.VehicleMakeId.ToString(),
                    Text = vm.Name
                })
                .ToList();
        }

        public async Task<List<SelectListItem>> GetVehicleModelsByMakeId(int makeId)
        {
            return await _context.VehicleModels
                .Where(m => m.VehicleMakeId == makeId)
                .OrderBy(m => m.Name)
                .Select(m => new SelectListItem
                {
                    Value = m.VehicleModelId.ToString(),
                    Text = m.Name

                })
                .ToListAsync();
        }


        //public async Task LoadMakes()
        //{
        //    VehicleMakeResponseDto vehicleMakeResponse = new();
        //    using (var client = new HttpClient())
        //    {
        //        var request = new HttpRequestMessage(HttpMethod.Get, "https://vpic.nhtsa.dot.gov/api/vehicles/GetAllMakes?format=json");
        //        var response = await client.SendAsync(request);
        //        response.EnsureSuccessStatusCode();
        //        var json = await response.Content.ReadAsStringAsync();
        //        vehicleMakeResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<VehicleMakeResponseDto>(json)!;
        //    }
        //    if (vehicleMakeResponse.Results.Count > 0)
        //    {
        //        List<VehicleMake> vehicleMakes = vehicleMakeResponse.Results.Select(v => new VehicleMake
        //        {
        //            IsActive = true,
        //            Name = v.MakeName,
        //            VehicleMakeId = v.MakeID
        //        }).ToList();
        //        await _context.VehicleMakes.AddRangeAsync(vehicleMakes);
        //        await _context.SaveChangesAsync();
        //    }
        //}

        //public async Task LoadModels()
        //{
        //    List<VehicleModel> models = new();

        //    VehicleMakeResponseDto vehicleMakeResponse = new();
        //    var scope = await _context.Database.BeginTransactionAsync();
        //    using (scope)
        //    {
        //        try
        //        {
        //            var makeQuery = _context.VehicleMakes.AsNoTracking().AsEnumerable();
        //            string makeName;

        //            var client = _clientFactory.CreateClient("VehicleModelsApi");

        //            HttpRequestMessage request = null;
        //            HttpResponseMessage response = null;
        //            string json = string.Empty;
        //            foreach (var make in makeQuery)

        //            {
        //                request = new HttpRequestMessage(HttpMethod.Get, $"{client.BaseAddress}GetModelsForMakeId/{make.VehicleMakeId}?format=json");
        //                response = await client.SendAsync(request);
        //                response.EnsureSuccessStatusCode();
        //                json = await response.Content.ReadAsStringAsync();
        //                vehicleMakeResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<VehicleMakeResponseDto>(json)!;
        //                if (vehicleMakeResponse.Results.Count > 0)
        //                {
        //                    models.AddRange(vehicleMakeResponse.Results.Select(x => new VehicleModel
        //                    {
        //                        Name = x.ModelName,
        //                        VehicleMakeId = make.VehicleMakeId,
        //                        VehicleModelId = x.ModelID
        //                    }));
        //                }
        //            }

        //            await _context.VehicleModels.AddRangeAsync(models);
        //            await _context.SaveChangesAsync();
        //            await scope.CommitAsync();
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Error loading vehicle models");
        //            await scope.RollbackAsync();
        //        }
        //     }


        //}

    }

}





