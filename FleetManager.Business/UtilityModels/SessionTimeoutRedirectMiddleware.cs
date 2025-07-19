using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.UtilityModels
{
    public class SessionTimeoutRedirectMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionTimeoutRedirectMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path;
            var method = context.Request.Method;
            var isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;

            var isLoginPath = path.Equals("/Account/Login", StringComparison.OrdinalIgnoreCase);
            var isAccessingStatic = path.StartsWithSegments("/css") ||
                                    path.StartsWithSegments("/js") ||
                                    path.StartsWithSegments("/lib") ||
                                    path.StartsWithSegments("/images") ||
                                    path == "/favicon.ico";
            var isApi = path.StartsWithSegments("/api");

            // Only redirect unauthenticated GET requests to secure pages
            if (!isAuthenticated &&
                method.Equals("GET", StringComparison.OrdinalIgnoreCase) &&
                !isLoginPath &&
                !isAccessingStatic &&
                !isApi)
            {
                var returnUrl = context.Request.Path + context.Request.QueryString;
                var loginUrl = $"/Account/Login?sessionExpired=true&returnUrl={Uri.EscapeDataString(returnUrl)}";
                context.Response.Redirect(loginUrl);
                return;
            }

            await _next(context);
        }
    }

}
