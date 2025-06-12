using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Enums
{
    public enum NotificationType
    {
        [Description("info")]
        Info = 0,
        [Description("success")]
        Success,
        [Description("warning")]
        Warning,
        [Description("error")]
        Error,
        [Description("vehicle")]
        Vehicle,
        [Description("maintenance")]
        Maintenance,
        [Description("driver")]
        Driver,
        [Description("alert")]
        Alert
    }
}
