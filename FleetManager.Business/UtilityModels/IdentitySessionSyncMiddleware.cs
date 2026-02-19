using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.UtilityModels
{
    public class IdentitySessionSyncMiddleware
    {
        private readonly RequestDelegate _next;

        public IdentitySessionSyncMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                context.Session.Clear();
            }

            await _next(context);
        }
    }
}
