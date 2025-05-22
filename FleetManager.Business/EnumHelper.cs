using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
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
    }
}
