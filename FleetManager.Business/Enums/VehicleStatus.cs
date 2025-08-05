using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Enums
{
    public enum VehicleStatus
    {
        [Description("Active")]
        Active = 1,

        [Description("Under Maintenance")]
        UnderMaintenance,

        [Description("Inactive")]
        Inactive
    }
}
