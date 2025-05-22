using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Enums
{
    public enum LicenseCategory
    {
        [Description("A - Motorcycles")]
        A,

        [Description("B - Cars & Light Vehicles")]
        B,

        [Description("C - Medium Trucks")]
        C,

        [Description("D - Heavy Trucks")]
        D,

        [Description("E - Buses & Passenger Vehicles")]
        E
    }
}
