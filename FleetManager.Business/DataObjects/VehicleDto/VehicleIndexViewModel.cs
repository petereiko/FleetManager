using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.VehicleDto
{
    public class VehicleIndexViewModel
    {
        public VehicleFilterDto Filter { get; set; } = new();
        public List<VehicleListItemDto> Vehicles { get; set; } = new();
        public PaginationDto Pagination { get; set; }
    }
}
