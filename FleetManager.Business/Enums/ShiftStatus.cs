using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Enums
{
    public enum ShiftStatus
    {
        [Display(Name = "On Duty")]
        OnDuty = 1,
        [Display(Name = "Off Duty")]
        OffDuty,

    }
}
