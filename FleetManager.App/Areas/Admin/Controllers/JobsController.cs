using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,CompanyAdmin")] 
    public class JobsController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
    }

    // Dashboard auth filter to restrict who can view Hangfire dashboard
    public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            return httpContext.User.Identity?.IsAuthenticated == true &&
                   (httpContext.User.IsInRole("SuperAdmin") || httpContext.User.IsInRole("CompanyAdmin"));
        }
    }
}
