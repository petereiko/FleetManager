using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.FineAndTollModule;
using FleetManager.Business.Interfaces.ManageDriverModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.Interfaces.VehicleModule;
using FleetManager.Business.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.App.Areas.Admin.Controllers
{
    // ─── AdminFineTollController ────────────────────────────────────────────────
    [Area("Admin")]
    [Authorize(Roles = "Company Admin,Company Owner,Super Admin")]
    public class ManageFineTollController : Controller
    {
        private readonly IFineAndTollService _fineService;
        private readonly IAuthUser _authUser;
        private readonly ILogger<ManageFineTollController> _logger;

        public ManageFineTollController(
            IFineAndTollService fineService,
            IAuthUser authUser,
            ILogger<ManageFineTollController> logger)
        {
            _fineService = fineService;
            _authUser = authUser;
            _logger = logger;
        }

        // ─── INDEX ────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            try
            {
                var roles = (_authUser.Roles ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim());
                bool isGlobal = roles.Contains("Company Owner") || roles.Contains("Super Admin");

                var query = _fineService
                    .QueryByBranch(isGlobal ? null : _authUser.CompanyBranchId);

                var allStatusOptions = _fineService.GetFineStatusOptions();

                var list = await query
                    .OrderByDescending(f => f.CreatedDate)
                    .Select(d => new FineTollListItemViewModel
                    {
                        Id = d.Id,
                        DateLogged = d.CreatedDate,
                        DriverName = d.DriverName,
                        Type = d.Type.ToString(),
                        Title = d.Title,
                        Amount = d.Amount,
                        Currency = d.Currency,
                        Status = d.Status.ToString(),
                        VehicleDescription = d.VehicleDescription,
                        IsMinimal = d.IsMinimal,
                        StatusOptions = allStatusOptions
                    })
                    .ToListAsync();

                return View(list);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading fine/toll list for admin");
                return View("Error");
            }
        }

        // ─── DETAILS (full or partial) ─────────────────────────────────────────
        public async Task<IActionResult> Details(long id)
        {
            try
            {
                var dto = await _fineService.GetByIdAsync(id);
                if (dto == null) return NotFound();

                var vm = new FineTollDetailsViewModel(dto);

                // If AJAX, return only the partial
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return PartialView("_DetailsPartial", vm);

                return View(vm);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching fine/toll {Id}", id);
                return View("Error");
            }
        }

        // ─── UPDATE STATUS (full form or AJAX) ─────────────────────────────────
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(long id, FineTollStatus status)
        {
            try
            {
                var result = await _fineService.UpdateStatusAsync(
                    id, status, _authUser.UserId);

                if (!result.Success)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return BadRequest(result.Message);

                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction(nameof(Index));
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Ok();

                TempData["SuccessMessage"] = "Fine/Toll status updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return StatusCode(403);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for fine/toll {Id}", id);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return StatusCode(500);
                TempData["ErrorMessage"] = "An unexpected error occurred.";
                return RedirectToAction(nameof(Index));
            }
        }
    }


}
