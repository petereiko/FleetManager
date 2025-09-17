using FleetManager.Business.Interfaces.MaintenanceModule;
using FleetManager.Business.UtilityModels.PdfService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace FleetManager.App.Controllers
{
    [Route("invoice")]
    public class InvoiceController : Controller
    {
        private readonly IRazorViewToStringRenderer _razorRenderer;
        private readonly IPdfService _pdfService;
        private readonly IMaintenanceService _maintenance;
        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<InvoiceController> _logger;

        // Tweak these defaults as you like
        private const int DefaultMaxWidth = 400;
        private const int DefaultMaxHeight = 120;
        private const int DefaultJpegQuality = 75;
        private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromHours(24);

        public InvoiceController(
            IRazorViewToStringRenderer razorRenderer,
            IPdfService pdfService,
            IMaintenanceService maintenance,
            IMemoryCache cache,
            IHttpClientFactory httpClientFactory,
            ILogger<InvoiceController> logger)
        {
            _razorRenderer = razorRenderer;
            _pdfService = pdfService;
            _maintenance = maintenance;
            _cache = cache;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet("download/{id}")]
        public async Task<IActionResult> Download(long id)
        {
            // 1) get the DTO you bind to your view
            var model = await _maintenance.GetTicketByIdAsync(id);
            if (model == null) return NotFound();

            // 2) ensure we have a CompanyLogoDataUrl — do conversion here (with caching)
            try
            {
                if (string.IsNullOrEmpty(model.CompanyLogoDataUrl) && !string.IsNullOrEmpty(model.CompanyLogoUrl))
                {
                    string cacheKey = $"company-logo-data:{model.CompanyLogoUrl}";
                    if (!_cache.TryGetValue<string>(cacheKey, out var dataUrl))
                    {
                        // Not cached — load, process, encode
                        var logoBytes = await LoadLogoBytesAsync(model.CompanyLogoUrl);
                        if (logoBytes != null && logoBytes.Length > 0)
                        {
                            dataUrl = await ResizeCompressAndConvertToDataUrlAsync(
                                logoBytes,
                                DefaultMaxWidth,
                                DefaultMaxHeight,
                                DefaultJpegQuality);

                            if (!string.IsNullOrEmpty(dataUrl))
                            {
                                // Cache the processed data-uri (so we don't repeat work)
                                var cacheEntryOptions = new MemoryCacheEntryOptions
                                {
                                    AbsoluteExpirationRelativeToNow = DefaultCacheDuration,
                                    SlidingExpiration = TimeSpan.FromHours(6)
                                };
                                _cache.Set(cacheKey, dataUrl, cacheEntryOptions);
                            }
                        }
                        else
                        {
                            _logger.LogInformation("Logo bytes not found for CompanyLogoUrl='{LogoUrl}' (ticket {TicketId})", model.CompanyLogoUrl, id);
                        }
                    }

                    if (!string.IsNullOrEmpty(dataUrl))
                    {
                        model.CompanyLogoDataUrl = dataUrl;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but don't prevent PDF generation
                _logger.LogWarning(ex, "Failed to prepare company logo for ticket {TicketId}", id);
            }

            // 3) render the view to HTML
            string html = await _razorRenderer.RenderViewToStringAsync("Print", model);

            // 4) convert to PDF
            var pdf = await _pdfService.GeneratePdfFromHtmlPuppeteer(html);

            // 5) return file
            return File(pdf, "application/pdf", $"Invoice_{SanitizeFileName(model.Subject)}.pdf");
        }

        // ----------------- Helpers -----------------

        private static string SanitizeFileName(string? input)
        {
            if (string.IsNullOrEmpty(input)) return "invoice";
            foreach (var c in Path.GetInvalidFileNameChars()) input = input.Replace(c, '_');
            return input;
        }

        /// <summary>
        /// Loads bytes from either:
        /// - absolute http(s) url (via IHttpClientFactory)
        /// - local path under wwwroot (~/ or / relative)
        /// - absolute local file path (rare)
        /// </summary>
        private async Task<byte[]?> LoadLogoBytesAsync(string logoPathOrUrl)
        {
            // 1) Remote absolute URL
            if (Uri.TryCreate(logoPathOrUrl, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                // Security: you can further validate uri.Host here (whitelist) if you wish.
                try
                {
                    var client = _httpClientFactory.CreateClient();
                    client.Timeout = TimeSpan.FromSeconds(10);
                    return await client.GetByteArrayAsync(uri);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch remote logo at {Url}", logoPathOrUrl);
                    return null;
                }
            }

            // 2) Relative path inside wwwroot (~/uploads/logo.png or /uploads/logo.png or uploads/logo.png)
            var rel = logoPathOrUrl.TrimStart('~').TrimStart('/');
            var wwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var absoluteFile = Path.Combine(wwwroot, rel.Replace('/', Path.DirectorySeparatorChar));

            if (System.IO.File.Exists(absoluteFile))
            {
                try
                {
                    return await System.IO.File.ReadAllBytesAsync(absoluteFile);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read local logo file {FilePath}", absoluteFile);
                    return null;
                }
            }

            // 3) If it starts with "/" but not readable on filesystem, try requesting it via app URL (if accessible)
            try
            {
                if (logoPathOrUrl.StartsWith("/"))
                {
                    var request = HttpContext.Request;
                    var absoluteUrl = $"{request.Scheme}://{request.Host}{logoPathOrUrl}";
                    var client = _httpClientFactory.CreateClient();
                    client.Timeout = TimeSpan.FromSeconds(10);
                    return await client.GetByteArrayAsync(absoluteUrl);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to fetch logo via app URL for {Path}", logoPathOrUrl);
            }

            _logger.LogInformation("Could not locate logo for pathOrUrl: {PathOrUrl}", logoPathOrUrl);
            return null;
        }

        /// <summary>
        /// Resize & compress image and return data URI. Preserves transparency by using PNG if image has alpha.
        /// Uses SixLabors.ImageSharp.
        /// </summary>
        private static async Task<string?> ResizeCompressAndConvertToDataUrlAsync(
            byte[] inputBytes, int maxWidth, int maxHeight, int jpegQuality)
        {
            try
            {
                // Load as Image<Rgba32>
                using var image = Image.Load<Rgba32>(inputBytes);

                // Resize (preserve aspect ratio, no upscaling)
                var width = image.Width;
                var height = image.Height;
                var widthRatio = (double)maxWidth / width;
                var heightRatio = (double)maxHeight / height;
                var ratio = Math.Min(1.0, Math.Min(widthRatio, heightRatio));
                var targetWidth = (int)Math.Round(width * ratio);
                var targetHeight = (int)Math.Round(height * ratio);

                if (targetWidth > 0 && targetHeight > 0 && (targetWidth != width || targetHeight != height))
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(targetWidth, targetHeight),
                        Mode = ResizeMode.Max
                    }));
                }

                // Detect alpha/transparency
                bool hasAlpha = image.PixelType.AlphaRepresentation != PixelAlphaRepresentation.None;

                using var ms = new MemoryStream();
                string mime;
                if (hasAlpha)
                {
                    await image.SaveAsPngAsync(ms);
                    mime = "image/png";
                }
                else
                {
                    var encoder = new JpegEncoder { Quality = Math.Clamp(jpegQuality, 10, 100) };
                    await image.SaveAsJpegAsync(ms, encoder);
                    mime = "image/jpeg";
                }

                var outBytes = ms.ToArray();
                return $"data:{mime};base64,{Convert.ToBase64String(outBytes)}";
            }
            catch
            {
                return null;
            }
        }
    }




    //public class InvoiceController : Controller
    //{
    //    private readonly IRazorViewToStringRenderer _razorRenderer;
    //    private readonly IPdfService _pdfService;
    //    private readonly IMaintenanceService _maintenance; // your existing service to get the DTO

    //    public InvoiceController(
    //        IRazorViewToStringRenderer razorRenderer,
    //        IPdfService pdfService,
    //        IMaintenanceService maintenance)
    //    {
    //        _razorRenderer = razorRenderer;
    //        _pdfService = pdfService;
    //        _maintenance = maintenance;
    //    }

    //    [HttpGet("download/{id}")]
    //    public async Task<IActionResult> Download(long id)
    //    {
    //        // 1) get the same DTO you bind to your view
    //        var model = await _maintenance.GetTicketByIdAsync(id);

    //        // 2) render the **same** Razor InvoicePartial (or reuse your full page view) to HTML
    //        string html = await _razorRenderer.RenderViewToStringAsync("Print", model);
    //        // (if you’re using the full view, use its path: e.g. "Maintenance/TicketDetails")

    //        // 3) convert to PDF
    //        //byte[] pdf = _pdfService.GeneratePdfFromHtml(html);

    //        var pdf = await _pdfService.GeneratePdfFromHtmlPuppeteer(html);

    //        // 4) return a FileResult to trigger browser download
    //        return File(pdf, "application/pdf", $"Invoice_{model.Subject}.pdf");
    //    }
    //}
}
