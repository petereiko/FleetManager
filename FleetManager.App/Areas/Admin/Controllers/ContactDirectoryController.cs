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

        private void PopulateCategories()
        {
            var list = _service.GetCategoryOptions();
            ViewBag.Categories = new SelectList(list, "Value", "Text");
        }
    }
}

