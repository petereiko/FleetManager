using FleetManager.Business.DataObjects.ApiModels;
using FleetManager.Business.DataObjects.MaintenanceDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.DriverVehicleModule;
using FleetManager.Business.Interfaces.MaintenanceModule;
using FleetManager.Business.Interfaces.UserModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.App.Controllers
{
    [Route("api/driver/maintenance")]
    [ApiController]
    [Authorize(Policy = "DriverApi")]
    public class DriverMaintenanceApiController : ControllerBase
    {
        private readonly IMaintenanceService _maintenanceService;
        private readonly IDriverVehicleService _assignmentService;
        private readonly IAuthUser _authUser;
        private readonly ILogger<DriverMaintenanceApiController> _logger;

        public DriverMaintenanceApiController(
            IMaintenanceService maintenanceService,
            IDriverVehicleService assignmentService,
            IAuthUser authUser,
            ILogger<DriverMaintenanceApiController> logger)
        {
            _maintenanceService = maintenanceService;
            _assignmentService = assignmentService;
            _authUser = authUser;
            _logger = logger;
        }

        #region Maintenance Tickets

        /// <summary>
        /// Get all maintenance tickets for the current driver
        /// </summary>
        [HttpGet("tickets")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedApiResponse<MaintenanceTicketResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTickets([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var driverId = await _assignmentService.GetDriverIdByUserAsync(_authUser.UserId);

                if (driverId == 0)
                {
                    return Ok(new ApiResponse<PaginatedApiResponse<MaintenanceTicketResponse>>
                    {
                        Success = false,
                        Message = "Driver profile not found"
                    });
                }

                var response = await _maintenanceService.QueryTicketsByDriverAsync(page, pageSize, driverId);

                if (!response.Success)
                {
                    return Ok(new ApiResponse<PaginatedApiResponse<MaintenanceTicketResponse>>
                    {
                        Success = false,
                        Message = response.Message
                    });
                }

                var tickets = response.Result.Items.Select(t => MapToTicketResponse(t)).ToList();

                var paginatedResponse = new PaginatedApiResponse<MaintenanceTicketResponse>
                {
                    Items = tickets,
                    Page = response.Result.Page,
                    PageSize = response.Result.PageSize,
                    TotalCount = response.Result.TotalCount,
                    TotalPages = response.Result.TotalPages
                };

                return Ok(new ApiResponse<PaginatedApiResponse<MaintenanceTicketResponse>>
                {
                    Success = true,
                    Message = $"Found {tickets.Count} ticket(s)",
                    Data = paginatedResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving maintenance tickets");
                return StatusCode(500, new ApiResponse<PaginatedApiResponse<MaintenanceTicketResponse>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving tickets"
                });
            }
        }

        /// <summary>
        /// Get a specific maintenance ticket by ID
        /// </summary>
        [HttpGet("tickets/{id}")]
        [ProducesResponseType(typeof(ApiResponse<MaintenanceTicketResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTicket(long id)
        {
            try
            {
                var driverId = await _assignmentService.GetDriverIdByUserAsync(_authUser.UserId);
                var ticket = await _maintenanceService.GetTicketByIdAsync(id);

                if (ticket == null)
                {
                    return NotFound(new ApiResponse<MaintenanceTicketResponse>
                    {
                        Success = false,
                        Message = "Ticket not found"
                    });
                }

                // Verify ownership
                if (ticket.DriverId != driverId)
                {
                    _logger.LogWarning("Driver {DriverId} attempted to access ticket {TicketId} not belonging to them",
                        driverId, id);
                    return Forbid();
                }

                return Ok(new ApiResponse<MaintenanceTicketResponse>
                {
                    Success = true,
                    Message = "Ticket retrieved",
                    Data = MapToTicketResponse(ticket)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ticket {Id}", id);
                return StatusCode(500, new ApiResponse<MaintenanceTicketResponse>
                {
                    Success = false,
                    Message = "An error occurred"
                });
            }
        }

        /// <summary>
        /// Create a new maintenance ticket
        /// </summary>
        [HttpPost("create-ticket")]
        [ProducesResponseType(typeof(ApiResponse<MaintenanceTicketResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateTicket([FromBody] MaintenanceTicketRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse<MaintenanceTicketResponse>
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                var driverId = await _assignmentService.GetDriverIdByUserAsync(_authUser.UserId);

                if (driverId == 0)
                {
                    return BadRequest(new ApiResponse<MaintenanceTicketResponse>
                    {
                        Success = false,
                        Message = "Driver profile not found"
                    });
                }

                // Verify vehicle is assigned to driver
                var isVehicleAssigned = await _assignmentService
                    .QueryAssignmentsByDriver(driverId)
                    .AnyAsync(a => a.VehicleId == request.VehicleId);

                if (!isVehicleAssigned)
                {
                    return BadRequest(new ApiResponse<MaintenanceTicketResponse>
                    {
                        Success = false,
                        Message = "Vehicle is not assigned to you"
                    });
                }

                // Validate priority
                if (!Enum.TryParse<MaintenancePriority>(request.Priority, out var priority))
                {
                    return BadRequest(new ApiResponse<MaintenanceTicketResponse>
                    {
                        Success = false,
                        Message = $"Invalid priority: {request.Priority}"
                    });
                }

                // Build input DTO
                var input = new MaintenanceTicketInputDto
                {
                    DriverId = driverId,
                    VehicleId = request.VehicleId,
                    Subject = request.Subject,
                    Notes = request.Notes,
                    Priority = priority,
                    Items = request.Items.Select(i => new MaintenanceTicketItemInputDto
                    {
                        PartCategoryId = i.PartCategoryId,
                        PartId = i.PartId,
                        CustomDescription = i.CustomDescription,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList()
                };

                var result = await _maintenanceService.CreateTicketAsync(input, _authUser.UserId);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse<MaintenanceTicketResponse>
                    {
                        Success = false,
                        Message = result.Message
                    });
                }

                var response = MapToTicketResponse(result.Result);

                return CreatedAtAction(
                    nameof(GetTicket),
                    new { id = response.Id },
                    new ApiResponse<MaintenanceTicketResponse>
                    {
                        Success = true,
                        Message = "Maintenance ticket created successfully",
                        Data = response
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating maintenance ticket");
                return StatusCode(500, new ApiResponse<MaintenanceTicketResponse>
                {
                    Success = false,
                    Message = "An error occurred while creating ticket"
                });
            }
        }

        #endregion

        #region Invoices

        /// <summary>
        /// Get all invoices for the current driver
        /// </summary>
        [HttpGet("invoices")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedApiResponse<InvoiceResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInvoices([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var driverId = await _assignmentService.GetDriverIdByUserAsync(_authUser.UserId);

                if (driverId == 0)
                {
                    return Ok(new ApiResponse<PaginatedApiResponse<InvoiceResponse>>
                    {
                        Success = false,
                        Message = "Driver profile not found"
                    });
                }

                var response = await _maintenanceService.QueryInvoicesByDriverAsync(page, pageSize, driverId);

                if (!response.Success)
                {
                    return Ok(new ApiResponse<PaginatedApiResponse<InvoiceResponse>>
                    {
                        Success = false,
                        Message = response.Message
                    });
                }

                var invoices = response.Result.Items.Select(i => MapToInvoiceResponse(i)).ToList();

                var paginatedResponse = new PaginatedApiResponse<InvoiceResponse>
                {
                    Items = invoices,
                    Page = response.Result.Page,
                    PageSize = response.Result.PageSize,
                    TotalCount = response.Result.TotalCount,
                    TotalPages = response.Result.TotalPages
                };

                return Ok(new ApiResponse<PaginatedApiResponse<InvoiceResponse>>
                {
                    Success = true,
                    Message = $"Found {invoices.Count} invoice(s)",
                    Data = paginatedResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices");
                return StatusCode(500, new ApiResponse<PaginatedApiResponse<InvoiceResponse>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving invoices"
                });
            }
        }

        /// <summary>
        /// Get a specific invoice by ID
        /// </summary>
        [HttpGet("invoices/{id}")]
        [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInvoice(long id)
        {
            try
            {
                var invoice = await _maintenanceService.GetInvoiceByIdAsync(id);

                if (invoice == null)
                {
                    return NotFound(new ApiResponse<InvoiceResponse>
                    {
                        Success = false,
                        Message = "Invoice not found"
                    });
                }

                // Note: Additional ownership verification could be added here if needed

                return Ok(new ApiResponse<InvoiceResponse>
                {
                    Success = true,
                    Message = "Invoice retrieved",
                    Data = MapToInvoiceResponse(invoice)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice {Id}", id);
                return StatusCode(500, new ApiResponse<InvoiceResponse>
                {
                    Success = false,
                    Message = "An error occurred"
                });
            }
        }

        #endregion

        #region Dropdown Options

        /// <summary>
        /// Get part categories for dropdown
        /// </summary>
        [HttpGet("part-categories")]
        [ProducesResponseType(typeof(ApiResponse<List<PartCategoryResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPartCategories()
        {
            try
            {
                var categories = await _maintenanceService.GetPartCategoriesAsync();

                var response = categories.Select(c => new PartCategoryResponse
                {
                    Id = int.Parse(c.Value),
                    Name = c.Text
                }).ToList();

                return Ok(new ApiResponse<List<PartCategoryResponse>>
                {
                    Success = true,
                    Message = $"Found {response.Count} part categor{(response.Count == 1 ? "y" : "ies")}",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving part categories");
                return StatusCode(500, new ApiResponse<List<PartCategoryResponse>>
                {
                    Success = false,
                    Message = "An error occurred"
                });
            }
        }

        /// <summary>
        /// Get parts by category for dropdown
        /// </summary>
        [HttpGet("part-categories/{categoryId}/parts")]
        [ProducesResponseType(typeof(ApiResponse<List<PartResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPartsByCategory(int categoryId)
        {
            try
            {
                var parts = await _maintenanceService.GetPartsByCategoryAsync(categoryId);

                var response = parts.Select(p => new PartResponse
                {
                    Id = int.Parse(p.Value),
                    Name = p.Text,
                    CategoryId = categoryId
                }).ToList();

                return Ok(new ApiResponse<List<PartResponse>>
                {
                    Success = true,
                    Message = $"Found {response.Count} part(s)",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving parts for category {CategoryId}", categoryId);
                return StatusCode(500, new ApiResponse<List<PartResponse>>
                {
                    Success = false,
                    Message = "An error occurred"
                });
            }
        }

        /// <summary>
        /// Get priority options for dropdown
        /// </summary>
        [HttpGet("priorities")]
        [ProducesResponseType(typeof(ApiResponse<List<PriorityOption>>), StatusCodes.Status200OK)]
        public IActionResult GetPriorities()
        {
            var priorities = new List<PriorityOption>
            {
                new PriorityOption { Value = 1, Text = "Low", Color = "#10B981" }, // Green
                new PriorityOption { Value = 2, Text = "Moderate", Color = "#F59E0B" }, // Yellow
                new PriorityOption { Value = 3, Text = "High", Color = "#EF4444" }, // Red
                new PriorityOption { Value = 4, Text = "Urgent", Color = "#DC2626" } // Dark Red
            };

            return Ok(new ApiResponse<List<PriorityOption>>
            {
                Success = true,
                Message = "Priorities retrieved",
                Data = priorities
            });
        }

        #endregion

        #region Helper Methods

        private MaintenanceTicketResponse MapToTicketResponse(MaintenanceTicketDto dto)
        {
            return new MaintenanceTicketResponse
            {
                Id = dto.Id,
                VehicleId = dto.VehicleId,
                VehicleDescription = dto.VehicleDescription,
                Subject = dto.Subject,
                Notes = dto.Notes,
                Status = dto.Status.ToString(),
                Priority = dto.Priority?.ToString() ?? "Moderate",
                CreatedAt = dto.CreatedAt,
                ResolvedAt = dto.ResolvedAt,
                AdminNotes = dto.AdminNotes,
                Items = dto.Items.Select(i => new MaintenanceTicketItemResponse
                {
                    Id = i.Id,
                    PartCategoryName = i.PartCategoryName ?? string.Empty,
                    PartName = i.PartName ?? string.Empty,
                    CustomDescription = i.CustomDescription,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.LineTotal
                }).ToList(),
                Invoice = dto.Invoice != null ? MapToInvoiceResponse(dto.Invoice) : null
            };
        }

        private InvoiceResponse MapToInvoiceResponse(InvoiceDto dto)
        {
            return new InvoiceResponse
            {
                Id = dto.Id,
                TicketId = dto.TicketId,
                InvoiceDate = dto.InvoiceDate,
                Status = dto.Status.ToString(),
                TotalAmount = dto.TotalAmount,
                Items = dto.Items.Select(i => new InvoiceItemResponse
                {
                    Id = i.Id,
                    PartName = i.PartName,
                    PartCategory = i.PartCategory,
                    CustomPartDescription = i.CustomPartDescription,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.LineTotal
                }).ToList()
            };
        }

        #endregion
    }

}
