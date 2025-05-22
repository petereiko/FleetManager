using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Enums
{
    //public enum EmploymentStatus
    //{
    //    Active =1,
    //    Inactive
    //}

    public enum EmploymentStatus
    {
        [Description("Full Time")]
        FullTime = 1,

        [Description("Part Time")]
        PartTime,

        [Description("Contract")]
        Contract,

        [Description("Temporary")]
        Temporary
    }
}
