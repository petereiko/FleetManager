using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.VehicleDto
{
    public class VehicleListItemDto
    {
        public long Id { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public string PlateNo { get; set; }
        public string Status { get; set; }
        public string BranchName { get; set; }
        public string? MainImagePath { get; set; }
        public string Color { get; set; }
        public TransmissionType TransmissionType { get; set; }
        public DateTime? LastServiceDate { get; set; }
    }
}
