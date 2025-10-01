using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.TripsDto
{
    public class SimpleVehicleDto
    {
        public long Id { get; set; }                 // Vehicle primary key
        public string PlateNo { get; set; } = string.Empty;
        public string? Make { get; set; }
        public string? Model { get; set; }
        public string Display => $"{PlateNo} {(Make != null ? $"({Make} {Model})" : string.Empty)}";
    }
}
