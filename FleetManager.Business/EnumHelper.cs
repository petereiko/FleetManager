using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business
{
    public static class EnumHelper
    {
        /// <summary>
        /// Turn an enum T into a list of SelectListItem { Value = underlying int, Text = enum name }.
        /// </summary>
        public static IEnumerable<SelectListItem> ToSelectList<TEnum>() where TEnum : struct, Enum
        {
            return Enum.GetValues(typeof(TEnum))
                .Cast<TEnum>()
                .Select(e => new SelectListItem
                {
                    Value = Convert.ToInt32(e).ToString(),
                    Text = e.ToString()
                });
        }

        public static string GetDisplayName(this Enum enumValue)
        {
            var display = enumValue.GetType()
                .GetField(enumValue.ToString())
                ?.GetCustomAttributes(typeof(DisplayAttribute), false)
                .FirstOrDefault() as DisplayAttribute;

            return display?.Name ?? enumValue.ToString();
        }
    }
}
