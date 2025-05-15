using FleetManager.Business.DataObjects;
using FleetManager.Business.Interfaces.ComapyBranchModule;
using FleetManager.Business.Interfaces.CompanyModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FleetManager.App.Areas.Company.Controllers
{
    [Area("Company")]
    //[Authorize(Roles = "Company")]
    public class BranchController : Controller
    {
        private readonly IBranchService _branchService;
        private readonly ICompanyManagementService _companyService;

        public BranchController(IBranchService branchService, ICompanyManagementService companyService)
        {
            _branchService = branchService;
            _companyService = companyService;
        }

        public async Task<IActionResult> Index()
        {
            var user = _companyService.GetUserData();
            if (user == null || user.CompanyId == null)
            {
                return Unauthorized("You are not authorized.");
            }

            var branches = await _branchService.GetBranchesForCompanyAsync();
            return View(branches);
        }

        public async Task<IActionResult> Create()
        {
            await LoadStateDropdowns();
            return View(new CompanyBranchDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CompanyBranchDto dto)
        {
            if (!ModelState.IsValid)
            {
                await LoadStateDropdowns();
                return View(dto);
            }

            var user = _companyService.GetUserData();
            if (user == null || user.CompanyId == null)
                return Unauthorized();

            dto.CompanyId = user.CompanyId;
            var result = await _branchService.AddBranchAsync(dto);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                await LoadStateDropdowns();
                return View(dto);
            }

            TempData["SuccessMessage"] = "Branch created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(long id)
        {
            var branch = await _branchService.GetBranchByIdAsync(id);
            if (branch == null)
                return NotFound();

            await LoadStateDropdowns(branch.StateId);
            return View(branch);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CompanyBranchDto dto)
        {
            if (!ModelState.IsValid)
            {
                await LoadStateDropdowns(dto.StateId);
                return View(dto);
            }

            var result = await _branchService.UpdateBranchAsync(dto);
            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                await LoadStateDropdowns(dto.StateId);
                return View(dto);
            }

            TempData["SuccessMessage"] = "Branch updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _branchService.DeleteBranchAsync(id);

            // Fix: Check if the result is a boolean and handle accordingly
            if (!result)
            {
                TempData["ErrorMessage"] = "Failed to delete the branch.";
            }
            else
            {
                TempData["SuccessMessage"] = "Branch deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadStateDropdowns(long? selectedStateId = null)
        {
            var states = await _companyService.GetAllStatesAsync();
            var distinctStates = states
                .GroupBy(s => s.Id)
                .Select(g => g.First()) // remove duplicates by Id
                .ToList();

            ViewBag.States = new SelectList(distinctStates, "Id", "Name", selectedStateId);
        }

        [HttpGet]
        public async Task<IActionResult> GetLgasByState(long stateId)
        {
            var lgas = await _companyService.GetLgasByStateIdAsync(stateId);
            var result = lgas.Select(l => new { id = l.Id, name = l.Name }).ToList();
            return Json(result);
        }
    }
}

