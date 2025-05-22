using FleetManager.Business.DataObjects.VehicleDto;
using FleetManager.Business.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels
{
    public class DriverIndexViewModel
    {
        public List<DriverListItemDto> Drivers { get; set; }
        public PaginationDto Pagination { get; set; }
        public bool IsGlobal { get; set; }  
    }
}
