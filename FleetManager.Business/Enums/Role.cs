using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Enums
{
    public enum Role
    {
        [Description("Driver")]
        Driver = 1,

        [Description("Company Admin")]
        CompanyAdmin,

        [Description("Company Owner")]
        CompanyOwner,

        [Description("Super Admin")]
        SuperAdmin
    }
}
