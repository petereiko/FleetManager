using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Enums
{
    public enum DriverDocumentType
    {
        [Description("License Photo")]
        LicensePhoto = 1,

        [Description("Passport Photo")]
        PassportPhoto,

        [Description("Utility Bill")]
        UtilityBill,

        [Description("Other")]
        Other
    }
}
