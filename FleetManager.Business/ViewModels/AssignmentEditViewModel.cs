using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels
{
    public class AssignmentEditViewModel : AssignmentCreateViewModel
    {
        [Required] public long Id { get; set; }
    }
}
