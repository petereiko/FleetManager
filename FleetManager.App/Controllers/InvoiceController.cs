using FleetManager.Business.Interfaces.MaintenanceModule;
using FleetManager.Business.UtilityModels.PdfService;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Controllers
{
    [Route("invoice")]
    public class InvoiceController : Controller
    {
        private readonly IRazorViewToStringRenderer _razorRenderer;
        private readonly IPdfService _pdfService;
        private readonly IMaintenanceService _maintenance; // your existing service to get the DTO

        public InvoiceController(
            IRazorViewToStringRenderer razorRenderer,
            IPdfService pdfService,
            IMaintenanceService maintenance)
        {
            _razorRenderer = razorRenderer;
            _pdfService = pdfService;
            _maintenance = maintenance;
        }

        [HttpGet("download/{id}")]
        public async Task<IActionResult> Download(long id)
        {
            // 1) get the same DTO you bind to your view
            var model = await _maintenance.GetTicketByIdAsync(id);

            // 2) render the **same** Razor InvoicePartial (or reuse your full page view) to HTML
            string html = await _razorRenderer.RenderViewToStringAsync("Print", model);
            // (if you’re using the full view, use its path: e.g. "Maintenance/TicketDetails")

            // 3) convert to PDF
            //byte[] pdf = _pdfService.GeneratePdfFromHtml(html);

            var pdf = await _pdfService.GeneratePdfFromHtmlPuppeteer(html);

            // 4) return a FileResult to trigger browser download
            return File(pdf, "application/pdf", $"Invoice_{model.Invoice.Id}.pdf");
        }
    }
}
