using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.UtilityModels.PdfService
{
    public interface IRazorViewToStringRenderer
    {
        
        Task<string> RenderViewToStringAsync(string viewName, object model);
    }
}
