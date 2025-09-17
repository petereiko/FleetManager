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
        // Retry count for transient failures
        private const int MaxRetries = 2;

        public async Task<byte[]> GeneratePdfFromHtmlPuppeteer(string html)
        {
            var launchOptions = new LaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                "--no-sandbox",
                "--disable-setuid-sandbox",
                "--disable-dev-shm-usage",   // helps in small /dev/shm environments
                "--single-process",
                "--no-zygote",
                "--disable-gpu",
                "--disable-extensions"
            }
            };

            int attempt = 0;
            Exception? lastEx = null;

            while (attempt < MaxRetries)
            {
                attempt++;
                IBrowser? browser = null; // Change from Browser? to IBrowser? to match Puppeteer.LaunchAsync return type

                try
                {
                    browser = await Puppeteer.LaunchAsync(launchOptions);

                    using var page = await browser.NewPageAsync();

                    page.DefaultNavigationTimeout = 60000;
                    page.DefaultTimeout = 60000;

                    await page.SetContentAsync(html, new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                        Timeout = 60000
                    });

                    await Task.Delay(250);

                    await page.SetViewportAsync(new ViewPortOptions { Width = 1200, Height = 1600 });

                    var pdfBytes = await page.PdfDataAsync(new PdfOptions
                    {
                        Format = PaperFormat.A4,
                        MarginOptions = new MarginOptions { Top = "1cm", Bottom = "1cm", Left = "1cm", Right = "1cm" },
                        PrintBackground = true
                    });

                    await browser.CloseAsync();
                    browser = null;

                    return pdfBytes;
                }
                catch (PuppeteerSharp.TargetClosedException tce)
                {
                    lastEx = tce;
                    try
                    {
                        if (browser != null) await browser.CloseAsync();
                    }
                    catch { }

                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    try
                    {
                        if (browser != null) await browser.CloseAsync();
                    }
                    catch { }

                    break;
                }
            }

            throw new InvalidOperationException("Failed to generate PDF with Puppeteer after retries.", lastEx);
        }
    }





    //public class PuppeteerPdfService : IPdfService
    //{
    //    public async Task<byte[]> GeneratePdfFromHtmlPuppeteer(string html)
    //    {
    //        var browser = await Puppeteer.LaunchAsync(new LaunchOptions
    //        {
    //            Headless = true,
    //            Args = new[] { "--no-sandbox" }
    //        });

    //        using var page = await browser.NewPageAsync();

    //        await page.SetContentAsync(html, new NavigationOptions
    //        {
    //            WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
    //        });

    //        var pdfBytes = await page.PdfDataAsync(new PdfOptions
    //        {
    //            Format = PaperFormat.A4,
    //            MarginOptions = new MarginOptions { Top = "1cm", Bottom = "1cm" }
    //        });

    //        await browser.CloseAsync();

    //        return pdfBytes;
    //    }
    //}

}
