using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.UtilityModels
{
    public static class DateTimeUtils
    {
        /// <summary>
        /// Treats an incoming DateTime value (from client inputs like flatpickr: "2025-10-02T14:30", no TZ)
        /// as Local time and converts to UTC for storage/comparison.
        /// </summary>
        public static DateTime ToUtcFromLocal(DateTime local)
            => DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime();

        /// <summary>
        /// Nullable overload.
        /// </summary>
        public static DateTime? ToUtcFromLocal(DateTime? local)
            => local.HasValue ? ToUtcFromLocal(local.Value) : (DateTime?)null;

        /// <summary>
        /// Convert stored UTC DateTime back to local (use before displaying to users).
        /// </summary>
        public static DateTime ToLocalFromUtc(DateTime utc)
            => DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();

        /// <summary>
        /// Nullable overload.
        /// </summary>
        public static DateTime? ToLocalFromUtc(DateTime? utc)
            => utc.HasValue ? ToLocalFromUtc(utc.Value) : (DateTime?)null;
    }
}
