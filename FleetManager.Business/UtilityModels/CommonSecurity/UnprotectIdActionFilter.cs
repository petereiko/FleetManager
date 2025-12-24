using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.UtilityModels.CommonSecurity
{

    /// <summary>
    /// Global filter that inspects action parameters and attempts to unprotect route values
    /// when the incoming route token is non-numeric. It sets the decoded value into ActionArguments.
    /// Supports long/int/Guid/string (and nullable variants).
    /// </summary>
    public class UnprotectIdActionFilter : IActionFilter
    {
        private readonly IIdProtector _idProtector;

        public UnprotectIdActionFilter(IIdProtector idProtector)
        {
            _idProtector = idProtector ?? throw new ArgumentNullException(nameof(idProtector));
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var parameters = context.ActionDescriptor.Parameters;

            foreach (var param in parameters)
            {
                var paramType = param.ParameterType;

                // Only target primitive id-like types and strings
                bool isLong = paramType == typeof(long) || paramType == typeof(long?);
                bool isInt = paramType == typeof(int) || paramType == typeof(int?);
                bool isGuid = paramType == typeof(Guid) || paramType == typeof(Guid?);
                bool isString = paramType == typeof(string);

                if (!isLong && !isInt && !isGuid && !isString)
                    continue;

                // Try find route value by parameter name first, then fall back to "id"
                if (!context.RouteData.Values.TryGetValue(param.Name, out var rawVal) &&
                    !context.RouteData.Values.TryGetValue("id", out rawVal))
                    continue;

                var raw = rawVal?.ToString();
                if (string.IsNullOrWhiteSpace(raw)) continue;

                // If it's already numeric and the parameter expects numeric, leave it
                if ((isLong || isInt) && long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                {
                    // Normal numeric string — MVC will model bind it as usual.
                    continue;
                }

                // If it looks like an unprotected value for a string param, keep it
                if (isString)
                {
                    // If the route contains a protected token, try to unprotect to string
                    var unprotected = _idProtector.UnprotectIdToString(raw);
                    if (unprotected != null)
                    {
                        context.ActionArguments[param.Name] = unprotected;
                    }
                    else
                    {
                        // If it's not a protected token, allow the raw string through
                        context.ActionArguments[param.Name] = raw;
                    }
                    continue;
                }

                // At this point param expects numeric or Guid, but raw isn't a plain numeric string.
                // Try unprotect via IIdProtector.
                string? unprotectedString = _idProtector.UnprotectIdToString(raw) ?? _idProtector.UnprotectIdToString(raw); // try UnprotectToString (same)
                if (unprotectedString == null)
                {
                    // maybe the token is actually numeric but contained formatting; try numeric parse
                    if (isLong && long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLong))
                    {
                        context.ActionArguments[param.Name] = parsedLong;
                        continue;
                    }

                    // failed to decode -> 404
                    context.Result = new NotFoundResult();
                    return;
                }

                // Now we have an unprotected string — try to convert to the parameter type
                if (isLong)
                {
                    if (long.TryParse(unprotectedString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var lv))
                    {
                        context.ActionArguments[param.Name] = lv;
                        continue;
                    }
                    // can't parse to long -> 404
                    context.Result = new NotFoundResult();
                    return;
                }

                if (isInt)
                {
                    if (int.TryParse(unprotectedString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var iv))
                    {
                        context.ActionArguments[param.Name] = iv;
                        continue;
                    }
                    context.Result = new NotFoundResult();
                    return;
                }

                if (isGuid)
                {
                    if (Guid.TryParse(unprotectedString, out var gv))
                    {
                        context.ActionArguments[param.Name] = gv;
                        continue;
                    }
                    context.Result = new NotFoundResult();
                    return;
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // no-op
        }
    }

    //public class UnprotectIdActionFilter : IActionFilter
    //{
    //    private readonly IIdProtector _idProtector;


    //    public UnprotectIdActionFilter(IIdProtector idProtector)
    //    {
    //        _idProtector = idProtector;
    //    }


    //    public void OnActionExecuting(ActionExecutingContext context)
    //    {
    //        var parameters = context.ActionDescriptor.Parameters;


    //        foreach (var param in parameters)
    //        {
    //            var paramType = param.ParameterType;
    //            if (paramType != typeof(long) && paramType != typeof(long?))
    //                continue;


    //            if (!context.RouteData.Values.TryGetValue(param.Name, out var rawVal) &&
    //            !context.RouteData.Values.TryGetValue("id", out rawVal))
    //                continue;


    //            var raw = rawVal?.ToString();
    //            if (string.IsNullOrEmpty(raw)) continue;


    //            if (long.TryParse(raw, out _)) continue;


    //            var decoded = _idProtector.UnprotectId(raw);
    //            if (decoded.HasValue)
    //            {
    //                context.ActionArguments[param.Name] = decoded.Value;
    //            }
    //            else
    //            {
    //                context.Result = new NotFoundResult();
    //                return;
    //            }
    //        }
    //    }


    //    public void OnActionExecuted(ActionExecutedContext context)
    //    {
    //    }
    //}
}

