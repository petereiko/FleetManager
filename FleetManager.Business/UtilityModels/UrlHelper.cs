using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.UtilityModels
{
    public static class UrlHelper
    {
        /// <summary>
        /// Converts a relative file path to an absolute URL
        /// </summary>
        /// <param name="httpContext">Current HTTP context</param>
        /// <param name="relativePath">Relative path like "/VehicleImages/abc.jpg"</param>
        /// <returns>Full URL like "https://localhost:7008/VehicleImages/abc.jpg"</returns>
        public static string ToAbsoluteUrl(HttpContext httpContext, string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return string.Empty;

            // If it's already a full URL, return as-is
            if (relativePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                relativePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return relativePath;
            }

            // Build full URL from relative path
            var request = httpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";

            // Ensure path starts with /
            if (!relativePath.StartsWith("/"))
                relativePath = "/" + relativePath;

            return $"{baseUrl}{relativePath}";
        }
    }
}
