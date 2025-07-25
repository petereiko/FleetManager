using PuppeteerSharp;
using PuppeteerSharp.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.UtilityModels.PdfService
{

    public class PuppeteerPdfService : IPdfService
    {
        public async Task<byte[]> GeneratePdfFromHtmlPuppeteer(string html)
        {
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox" }
            });

            using var page = await browser.NewPageAsync();

            await page.SetContentAsync(html, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
            });

            var pdfBytes = await page.PdfDataAsync(new PdfOptions
            {
                Format = PaperFormat.A4,
                MarginOptions = new MarginOptions { Top = "1cm", Bottom = "1cm" }
            });

            await browser.CloseAsync();

            return pdfBytes;
        }
    }

}
