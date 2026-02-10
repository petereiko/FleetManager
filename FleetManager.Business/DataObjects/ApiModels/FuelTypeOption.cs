using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ApiModels
{
    public class FuelTypeOption
    {
        public int Value { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    // Helper model for vehicle dropdown
    public class VehicleOption
    {
        public long VehicleId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string PlateNo { get; set; } = string.Empty;
    }
}
