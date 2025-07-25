using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.UtilityModels.PdfService
{
    public class RazorViewToStringRenderer : IRazorViewToStringRenderer
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IActionContextAccessor _actionContextAccessor;

        public RazorViewToStringRenderer(
            IRazorViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IActionContextAccessor actionContextAccessor)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _actionContextAccessor = actionContextAccessor;
        }

        public async Task<string> RenderViewToStringAsync(string viewName, object model)
        {
            var actionContext = _actionContextAccessor.ActionContext
                               ?? throw new InvalidOperationException("No ActionContext available");

            var viewResult = _viewEngine.FindView(actionContext, viewName, isMainPage: false);
            if (!viewResult.Success)
                throw new InvalidOperationException($"Could not find view '{viewName}'.");

            var viewDictionary = new ViewDataDictionary(
                metadataProvider: new EmptyModelMetadataProvider(),
                modelState: actionContext.ModelState)
            {
                Model = model
            };

            var tempData = new TempDataDictionary(
                actionContext.HttpContext,
                _tempDataProvider);

            await using var sw = new StringWriter();
            var viewContext = new ViewContext(
                actionContext,
                viewResult.View,
                viewDictionary,
                tempData,
                sw,
                new HtmlHelperOptions()
            );

            await viewResult.View.RenderAsync(viewContext);
            return sw.ToString();
        }
    }
}
