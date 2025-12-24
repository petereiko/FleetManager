using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.UtilityModels.CommonSecurity
{
    public static class UrlHelperExtensions
    {
        public static string ActionWithProtectedId(this IUrlHelper url, string action, string controller, long id, object routeValues = null)
        {
            var idProtector = (IIdProtector)url.ActionContext.HttpContext.RequestServices.GetService(typeof(IIdProtector));
            var protectedId = idProtector?.ProtectId(id) ?? id.ToString();


            var rv = new RouteValueDictionary(routeValues ?? new { });
            rv["id"] = protectedId;
            return url.Action(action, controller, rv);
        }
    }
}
