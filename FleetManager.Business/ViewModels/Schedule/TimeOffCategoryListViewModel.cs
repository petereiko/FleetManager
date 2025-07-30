using FleetManager.Business.DataObjects.Schedule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.Schedule
{
    public class TimeOffCategoryListViewModel
    {
        public IEnumerable<TimeOffCategoryDto> Categories { get; set; } = Enumerable.Empty<TimeOffCategoryDto>();
    }
}
