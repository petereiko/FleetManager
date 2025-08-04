using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.ScheduleModule;
using FleetManager.Business.ViewModels.Schedule;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class TimeOffController : Controller
    {
        private readonly ITimeOffService _timeOff;
        private readonly ITimeOffCategoryService _categories;
        private readonly ILogger<TimeOffController> _logger;

        public TimeOffController(ITimeOffService timeOffService, ILogger<TimeOffController> logger, ITimeOffCategoryService categories)
        {
            _timeOff = timeOffService;
            _logger = logger;
            _categories = categories;
        }

        // GET: /Admin/TimeOff
        public async Task<IActionResult> Index()
        {
            try
            {
                var pending = await _timeOff.GetAllPendingRequestsAsync(null);
                var vm = new TimeOffPendingListViewModel { PendingRequests = pending };
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pending time‑off requests");
                return View("Error");
            }
        }

        // GET: /Admin/TimeOff/Review/5
        [HttpGet]
        public async Task<IActionResult> Review(long id)
        {
            try
            {
                var req = await _timeOff.GetRequestByIdAsync(id);
                if (req == null) return NotFound();
                var vm = new TimeOffReviewViewModel { Request = req };
                return PartialView("_ReviewPartial", vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading review for request #{Id}", id);
                return View("Error");
            }
        }

        // POST: /Admin/TimeOff/Approve/5
        //[HttpPost, ValidateAntiForgeryToken]
        //public async Task<IActionResult> Approve(long id, TimeOffReviewViewModel vm)
        //{
        //    try
        //    {
        //        await _timeOff.ApproveRequestAsync(id, vm.AdminComment);
        //        TempData["SuccessMessage"] = "Request approved.";
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error approving request #{Id}", id);
        //        TempData["ErrorMessage"] = "Could not approve.";
        //        return RedirectToAction(nameof(Index));
        //    }
        //}

        //// POST: /Admin/TimeOff/Deny/5
        //[HttpPost, ValidateAntiForgeryToken]
        //public async Task<IActionResult> Deny(long id, TimeOffReviewViewModel vm)
        //{
        //    try
        //    {
        //        await _timeOff.DenyRequestAsync(id, vm.AdminComment);
        //        TempData["SuccessMessage"] = "Request denied.";
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error denying request #{Id}", id);
        //        TempData["ErrorMessage"] = "Could not deny.";
        //        return RedirectToAction(nameof(Index));
        //    }
        //}

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(long id, TimeOffReviewViewModel vm)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid input." });
            try
            {
                await _timeOff.ApproveRequestAsync(id, vm.AdminComment);
                return Json(new
                {
                    success = true,
                    message = "Request approved.",
                    requestId = id,
                    newStatus = TimeOffStatus.Approved.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving request #{Id}", id);
                return Json(new { success = false, message = "Could not approve request." });
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Deny(long id, TimeOffReviewViewModel vm)
        {
            // even on ModelState errors, return Json
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid input." });

            try
            {
                await _timeOff.DenyRequestAsync(id, vm.AdminComment);
                return Json(new
                {
                    success = true,
                    message = "Request denied.",
                    requestId = id,
                    newStatus = TimeOffStatus.Denied.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error denying request #{Id}", id);
                return Json(new { success = false, message = "Could not deny request." });
            }
        }




        // ─── CATEGORY CRUD ─────────────────────────────────────────────────────

        // GET: /Admin/TimeOff/Categories
        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            try
            {
                var cats = await _categories.GetAllAsync();
                return View(new TimeOffCategoryListViewModel { Categories = cats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading time‑off categories");
                return View("Error");
            }
        }

        // GET: /Admin/TimeOff/CreateCategory
        [HttpGet]
        public IActionResult CreateCategory()
        {
            return View(new TimeOffCategoryInputViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(TimeOffCategoryInputViewModel vm)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

            var resp = await _categories.CreateAsync(vm.Name, vm.Description);
            if (!resp.Success)
                return BadRequest(new { success = false, message = resp.Message });

            return Json(new { success = true, message = "Category created.", category = new { resp.Result.Id, resp.Result.Name, resp.Result.Description } });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(TimeOffCategoryInputViewModel vm)
        {
            if (!ModelState.IsValid || vm.Id == null)
                return BadRequest(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

            var resp = await _categories.UpdateAsync(vm.Id.Value, vm.Name, vm.Description);
            if (!resp.Success)
                return BadRequest(new { success = false, message = resp.Message });

            return Json(new { success = true, message = "Category updated.", category = new { Id = vm.Id, vm.Name, vm.Description } });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(long id)
        {
            var resp = await _categories.DeleteAsync(id);
            if (!resp.Success)
                return BadRequest(new { success = false, message = resp.Message });

            return Json(new { success = true, message = "Category deleted.", id });
        }

        //// POST: /Admin/TimeOff/CreateCategory
        //[HttpPost, ValidateAntiForgeryToken]
        //public async Task<IActionResult> CreateCategory(TimeOffCategoryInputViewModel vm)
        //{
        //    if (!ModelState.IsValid)
        //        return View(vm);

        //    var resp = await _categories.CreateAsync(vm.Name, vm.Description);
        //    if (!resp.Success)
        //    {
        //        ModelState.AddModelError("", resp.Message);
        //        return View(vm);
        //    }

        //    TempData["SuccessMessage"] = "Category created.";
        //    return RedirectToAction(nameof(Categories));
        //}

        //// GET: /Admin/TimeOff/EditCategory/5
        //[HttpGet]
        //public async Task<IActionResult> EditCategory(long id)
        //{
        //    var dto = await _categories.GetByIdAsync(id);
        //    if (dto == null) return NotFound();

        //    var vm = new TimeOffCategoryInputViewModel
        //    {
        //        Id = dto.Id,
        //        Name = dto.Name,
        //        Description = dto.Description
        //    };
        //    return View(vm);
        //}

        //// POST: /Admin/TimeOff/EditCategory/5
        //[HttpPost, ValidateAntiForgeryToken]
        //public async Task<IActionResult> EditCategory(TimeOffCategoryInputViewModel vm)
        //{
        //    if (!ModelState.IsValid || vm.Id == null)
        //        return View(vm);

        //    var resp = await _categories.UpdateAsync(vm.Id.Value, vm.Name, vm.Description);
        //    if (!resp.Success)
        //    {
        //        ModelState.AddModelError("", resp.Message);
        //        return View(vm);
        //    }

        //    TempData["SuccessMessage"] = "Category updated.";
        //    return RedirectToAction(nameof(Categories));
        //}

        //// POST: /Admin/TimeOff/DeleteCategory/5
        //[HttpPost, ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteCategory(long id)
        //{
        //    var resp = await _categories.DeleteAsync(id);
        //    if (resp.Success)
        //        TempData["SuccessMessage"] = "Category deleted.";
        //    else
        //        TempData["ErrorMessage"] = resp.Message;

        //    return RedirectToAction(nameof(Categories));
        //}

    }
}
