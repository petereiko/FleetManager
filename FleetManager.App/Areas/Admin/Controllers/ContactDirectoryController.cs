using FleetManager.Business.DataObjects;
using FleetManager.Business.Interfaces.ContactDirectoryModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FleetManager.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class ContactDirectoryController : Controller
    {
        private readonly IContactDirectoryService _service;

        public ContactDirectoryController(IContactDirectoryService service)
        {
            _service = service;
        }

        // GET: /Vendor/ContactDirectory
        public async Task<IActionResult> Index()
        {
            var items = await _service.GetAllContactsAsync();
            PopulateCategories();
            return View(items);
        }

        // GET: /Vendor/ContactDirectory/Details/5
        public async Task<IActionResult> Details(long id)
        {
            var dto = await _service.GetContactByIdAsync(id);
            if (dto == null) return NotFound();
            return PartialView("_DetailsPartial", dto);
        }

        // GET: /Vendor/ContactDirectory/Create
        public IActionResult Create()
        {
            PopulateCategories();
            return View(new ContactDirectoryDto());
        }

        // POST: /Vendor/ContactDirectory/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContactDirectoryDto dto)
        {
            if (!ModelState.IsValid)
            {
                PopulateCategories();
                return View(dto);
            }

            var resp = await _service.AddContactAsync(dto);
            if (!resp.Success)
            {
                ModelState.AddModelError(string.Empty, resp.Message);
                PopulateCategories();
                return View(dto);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Vendor/ContactDirectory/Edit/5
        public async Task<IActionResult> Edit(long id)
        {
            var dto = await _service.GetContactByIdAsync(id);
            if (dto == null) return NotFound();
            PopulateCategories();
            return View(dto);
        }

        // POST: /Vendor/ContactDirectory/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ContactDirectoryDto dto)
        {
            if (!ModelState.IsValid)
            {
                PopulateCategories();
                return View(dto);
            }

            var resp = await _service.UpdateContactAsync(dto);
            if (!resp.Success)
            {
                ModelState.AddModelError(string.Empty, resp.Message);
                PopulateCategories();
                return View(dto);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Vendor/ContactDirectory/Delete/5
        public async Task<IActionResult> Delete(long id)
        {
            var dto = await _service.GetContactByIdAsync(id);
            if (dto == null) return NotFound();
            return View(dto);
        }

        // POST: /Vendor/ContactDirectory/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var resp = await _service.DeleteContactAsync(id);
            if (!resp.Success)
                TempData["Error"] = resp.Message;
            return RedirectToAction(nameof(Index));
        }

        // -------------------------
        // RATING ACTIONS (AJAX)
        // -------------------------

        // GET: /Vendor/ContactDirectory/Rate/5
        // returns a partial view with a small rating modal
        public async Task<IActionResult> Rate(long id)
        {
            var dto = await _service.GetContactByIdAsync(id);
            if (dto == null) return NotFound();

            // The partial should contain a small form to pick 1-5 stars.
            return PartialView("_RateModal", dto);
        }

        // POST: /Vendor/ContactDirectory/SubmitRating (AJAX)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRating(ContactRatingDto dto)
        {
            // DTO should include ContactId (or ContactId field name used in your DTO)
            var resp = await _service.AddOrUpdateRatingAsync(dto);

            if (!resp.Success)
            {
                return Json(new
                {
                    success = false,
                    message = resp.Message
                });
            }

            return Json(new
            {
                success = true,
                message = resp.Message ?? "Rating saved",
                contactId = resp.Result?.ContactId,
                average = resp.Result?.AverageRating,
                count = resp.Result?.RatingCount
            });
        }

        // POST: /Vendor/ContactDirectory/RemoveRating (AJAX)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRating(long contactId)
        {
            var resp = await _service.RemoveMyRatingAsync(contactId);

            if (!resp.Success)
            {
                return Json(new { success = false, message = resp.Message });
            }

            return Json(new
            {
                success = true,
                message = resp.Message ?? "Rating removed",
                contactId = resp.Result?.ContactId,
                average = resp.Result?.AverageRating,
                count = resp.Result?.RatingCount
            });
        }



        private void PopulateCategories()
        {
            var list = _service.GetCategoryOptions();
            ViewBag.Categories = new SelectList(list, "Value", "Text");
        }
    }
}

